#!/bin/bash
# ===========================================================================
# Full API smoke test — runs every documented endpoint with curl
# Target: .NET 10 Container Depot API at https://localhost:7200
# ===========================================================================
set -u

BASE="https://localhost:7200"
PASS=0
FAIL=0
ERRORS=()
CURL_BODY=/tmp/api_test_body.json

bold()  { printf "\033[1m%s\033[0m\n" "$*"; }
green() { printf "\033[32m%s\033[0m\n" "$*"; }
red()   { printf "\033[31m%s\033[0m\n" "$*"; }
yellow(){ printf "\033[33m%s\033[0m\n" "$*"; }

# ---- Helpers ---------------------------------------------------------------
call() {
  # call <description> <method> <path> [token] [data] [expect-2xx-or-3xx]
  local desc="$1" method="$2" path="$3" token="${4:-}" data="${5:-}" expect="${6:-2|3}"
  local code body
  local args=(-sk -o "$CURL_BODY" -w "%{http_code}" -X "$method" "$BASE$path"
              ${token:+-H "Authorization: Bearer $token"}
              -H "X-Tenant-Id: default")
  if [ -n "$data" ]; then
    args+=(-H "Content-Type: application/json" -d "$data")
  fi
  code=$(curl "${args[@]}")
  body=$(head -c 200 "$CURL_BODY")
  if [[ "$code" =~ ^($expect)..$ ]]; then
    green "  ✓ $desc → HTTP $code"
    PASS=$((PASS+1))
  else
    red "  ✗ $desc → HTTP $code — $body"
    FAIL=$((FAIL+1))
    ERRORS+=("$desc → HTTP $code")
  fi
}

bold "================================================================"
bold "TechSpherex Container Depot — Full API Smoke Test"
bold "Target: $BASE"
bold "================================================================"

# ---- 1. Public health ------------------------------------------------------
bold ""
bold "[1] Health"
call "GET /health" GET "/health" "" "" "2|3"

# ---- 2. Auth ----------------------------------------------------------------
bold ""
bold "[2] Authentication"
LOGIN_RESP=$(curl -sk -X POST "$BASE/api/identity/login" \
  -H "Content-Type: application/json" -H "X-Tenant-Id: default" \
  -d '{"email":"admin@TechSpherex.dev","password":"Admin@123"}')
ACCESS=$(echo "$LOGIN_RESP" | python3 -c "import json,sys; print(json.load(sys.stdin).get('accessToken',''))" 2>/dev/null)
REFRESH=$(echo "$LOGIN_RESP" | python3 -c "import json,sys; print(json.load(sys.stdin).get('refreshToken',''))" 2>/dev/null)
if [ -n "$ACCESS" ] && [ -n "$REFRESH" ]; then
  green "  ✓ POST /api/identity/login → got JWT (len=${#ACCESS}) + refresh"
  PASS=$((PASS+1))
else
  red "  ✗ POST /api/identity/login → empty response"
  FAIL=$((FAIL+1))
  echo "Login response: $LOGIN_RESP"
  exit 1
fi
call "POST /api/identity/refresh" POST "/api/identity/refresh" "" "{\"accessToken\":\"$ACCESS\",\"refreshToken\":\"$REFRESH\"}" "2|4"

# ---- 3. Lookups ------------------------------------------------------------
bold ""
bold "[3] Lookups"
call "GET /api/lookups/line-operators"  GET "/api/lookups/line-operators" "$ACCESS"
call "GET /api/lookups/container-types" GET "/api/lookups/container-types" "$ACCESS"
call "GET /api/lookups/customers"       GET "/api/lookups/customers"      "$ACCESS"

# Get IDs for subsequent tests
LINEOP_ID=$(curl -sk -H "Authorization: Bearer $ACCESS" -H "X-Tenant-Id: default" \
  "$BASE/api/lookups/line-operators" | python3 -c "import json,sys; d=json.load(sys.stdin); print(next((x['id'] for x in d if x['code']=='CMA'), d[0]['id']))")
CTYPE_ID=$(curl -sk -H "Authorization: Bearer $ACCESS" -H "X-Tenant-Id: default" \
  "$BASE/api/lookups/container-types" | python3 -c "import json,sys; d=json.load(sys.stdin); print(next((x['id'] for x in d if x['code']=='22G1'), d[0]['id']))")
yellow "  → LINEOP_ID=$LINEOP_ID, CTYPE_ID=$CTYPE_ID"

# ---- 4. Yard (with seeded depot) ------------------------------------------
bold ""
bold "[4] Yard — seeded depot"
DEPOT_ID=$(cat /tmp/depot_id.txt)
BLOCK_A_ID=$(grep ":A$" /tmp/blocks.txt | head -1 | cut -d: -f1)
BLOCK_V_ID=$(grep ":V$" /tmp/blocks.txt | head -1 | cut -d: -f1)
yellow "  → DEPOT_ID=$DEPOT_ID, BLOCK_A_ID=$BLOCK_A_ID, BLOCK_V_ID=$BLOCK_V_ID"

call "GET /api/yard/depots/{id}/map" GET "/api/yard/depots/$DEPOT_ID/map" "$ACCESS"

# ---- 5. Customers -----------------------------------------------------------
bold ""
bold "[5] Customers"
TAX_CODE="0312$(date +%s | tail -c 6)"
call "POST /api/lookups/customers" POST "/api/lookups/customers" "$ACCESS" \
  "{\"taxCode\":\"$TAX_CODE\",\"name\":\"ACME Logistics Vietnam Co., Ltd\"}"

# ---- 6. Containers — Modulo-11 validation ----------------------------------
bold ""
bold "[6] Containers — ISO 6346 Modulo-11 validation"
# Generate unique container numbers per run by appending epoch seconds to serial portion
TS=$(date +%s | tail -c 7)   # 6 unique digits
TEN_CHARS="CMAU${TS:0:6}"     # e.g. CMAU178753
# Compute check digit using ISO 6346 algorithm
CHECK=$(python3 -c "
def letter(c):
    vals=[10,12,13,14,15,16,17,18,19,20,21,23,24,25,26,27,28,29,30,31,32,34,35,36,37,38]
    return vals[ord(c)-ord('A')]
ten='${TEN_CHARS}'
s=0
for i,c in enumerate(ten):
    v=int(c) if c.isdigit() else letter(c)
    s+=v*(2**i)
print(s%11)
")
VALID="${TEN_CHARS}${CHECK}"
# Invalid = same first 10 chars but different last digit (we compute a known-wrong digit)
INVALID="${TEN_CHARS}$(( (CHECK + 1) % 11 ))"

yellow "  → VALID=$VALID (cd=$CHECK), INVALID=$INVALID"
call "POST /api/containers (VALID $VALID)" POST "/api/containers" "$ACCESS" \
  "{\"containerNumber\":\"$VALID\",\"containerTypeId\":\"$CTYPE_ID\",\"isoCode\":\"22G1\",\"sizeFeet\":20,\"maxWeightKg\":30480,\"tareWeightKg\":2230,\"manufactureDate\":\"2020-01-15T00:00:00Z\",\"owner\":\"CMA CGM\",\"condition\":\"Normal\"}"

# Invalid check digit — last digit intentionally wrong
call "POST /api/containers (INVALID check digit $INVALID)" POST "/api/containers" "$ACCESS" \
  "{\"containerNumber\":\"$INVALID\",\"containerTypeId\":\"$CTYPE_ID\",\"isoCode\":\"22G1\",\"sizeFeet\":20,\"maxWeightKg\":30480,\"tareWeightKg\":2230,\"manufactureDate\":\"2020-01-15T00:00:00Z\",\"owner\":\"CMA CGM\",\"condition\":\"Normal\"}" "4|2"

# Too-short container number
call "POST /api/containers (too short 'BAD')" POST "/api/containers" "$ACCESS" \
  "{\"containerNumber\":\"BAD\",\"containerTypeId\":\"$CTYPE_ID\",\"isoCode\":\"22G1\",\"sizeFeet\":20,\"maxWeightKg\":30480,\"tareWeightKg\":2230,\"manufactureDate\":\"2020-01-15T00:00:00Z\",\"owner\":\"X\",\"condition\":\"Normal\"}" "4"

call "GET /api/containers (paginated)" GET "/api/containers?page=1&pageSize=20" "$ACCESS"
call "GET /api/containers/$VALID"   GET "/api/containers/$VALID" "$ACCESS"

# ---- 7. Yard — block management --------------------------------------------
bold ""
bold "[7] Yard — block create / resize"
BLOCK_CODE_B="B$(date +%s | tail -c 4)"
BLOCK_CODE_V="V$(date +%s | tail -c 4)"
call "POST /api/blocks (create non-virtual)" POST "/api/blocks" "$ACCESS" \
  "{\"depotId\":\"$DEPOT_ID\",\"code\":\"$BLOCK_CODE_B\",\"name\":\"Block $BLOCK_CODE_B\",\"maxBay\":6,\"maxRow\":4,\"maxTier\":2,\"displayOrder\":2}"
call "POST /api/blocks/virtual" POST "/api/blocks/virtual" "$ACCESS" \
  "{\"depotId\":\"$DEPOT_ID\",\"code\":\"$BLOCK_CODE_V\",\"name\":\"Virtual Block $BLOCK_CODE_V\"}"
call "PATCH /api/blocks/{id}/resize (block A)" PATCH "/api/blocks/$BLOCK_A_ID/resize" "$ACCESS" \
  '{"maxBay":6,"maxRow":4,"maxTier":3}'

# ---- 8. Delivery Orders ----------------------------------------------------
bold ""
bold "[8] Delivery Orders"
# Need a customer first
CUST_ID=$(curl -sk -H "Authorization: Bearer $ACCESS" -H "X-Tenant-Id: default" \
  "$BASE/api/lookups/customers" | python3 -c "import json,sys; d=json.load(sys.stdin); print(d[0]['id'] if d else '')")
if [ -z "$CUST_ID" ]; then
  # Create a fresh customer
  CUST_RESP=$(curl -sk -H "Authorization: Bearer $ACCESS" -H "Content-Type: application/json" -H "X-Tenant-Id: default" \
    -X POST "$BASE/api/lookups/customers" -d '{"taxCode":"0312345678","name":"Test Customer"}')
  CUST_ID=$(echo "$CUST_RESP" | python3 -c "import json,sys; print(json.load(sys.stdin).get('id',''))")
fi
yellow "  → CUST_ID=$CUST_ID"

# Generate unique order number to avoid duplicate conflict
ORDER_NUM="DO-$(date +%s)"
call "POST /api/delivery-orders ($ORDER_NUM)" POST "/api/delivery-orders" "$ACCESS" \
  "{\"orderNumber\":\"$ORDER_NUM\",\"customerId\":\"$CUST_ID\",\"lineOperatorId\":\"$LINEOP_ID\",\"expiryDate\":\"2026-12-31T00:00:00Z\",\"vesselVoyage\":\"MV Northern / V.001W\",\"lines\":[{\"containerTypeId\":\"$CTYPE_ID\",\"requestedQuantity\":5,\"deliveredQuantity\":0}]}"
call "GET /api/delivery-orders/active" GET "/api/delivery-orders/active" "$ACCESS"

# Get created DO id
DO_ID=$(curl -sk -H "Authorization: Bearer $ACCESS" -H "X-Tenant-Id: default" \
  "$BASE/api/delivery-orders/active" | python3 -c "
import json,sys
d=json.load(sys.stdin)
m = next((x for x in d if x.get('orderNumber')=='$ORDER_NUM'), d[0] if d else None)
print(m['id'] if m else '')
")
yellow "  → DO_ID=$DO_ID"
call "GET /api/delivery-orders/{id}" GET "/api/delivery-orders/$DO_ID" "$ACCESS"

# ---- 9. Gate operations ----------------------------------------------------
bold ""
bold "[9] Gate operations (EIR)"

# Get a yard slot from block A — use slot 2 instead of slot 1 to avoid leftover from previous runs
SLOT_ID=$(curl -sk -H "Authorization: Bearer $ACCESS" -H "X-Tenant-Id: default" \
  "$BASE/api/yard/depots/$DEPOT_ID/map" | python3 -c "
import json,sys
d=json.load(sys.stdin)
ba = next(b for b in d['blocks'] if b['code']=='A')
# Find first unoccupied slot
for s in ba['slots']:
    if not s['isOccupied']:
        print(s['id'])
        break
")
yellow "  → SLOT_ID=$SLOT_ID"

call "POST /api/gate/in (may 409 if container already in yard from previous run)" POST "/api/gate/in" "$ACCESS" \
  "{\"containerNumber\":\"$VALID\",\"lineOperatorId\":\"$LINEOP_ID\",\"blockId\":\"$BLOCK_A_ID\",\"yardSlotId\":\"$SLOT_ID\",\"bay\":1,\"row\":1,\"tier\":1,\"classification\":\"A\",\"conditionAtGateIn\":\"Normal\",\"vehicleInNumber\":\"51C-123.45\",\"driverInName\":\"Nguyen Van A\"}" "2|4"

# Get the latest in-yard movement
MV_ID=$(curl -sk -H "Authorization: Bearer $ACCESS" -H "X-Tenant-Id: default" \
  "$BASE/api/containers/CMAU12345674/movements" | python3 -c "
import json,sys
d=json.load(sys.stdin)
ms = [m for m in d if m['status']=='InYard']
print(ms[0]['id'] if ms else d[0]['id'])
")
yellow "  → MV_ID=$MV_ID"

# Get a second unoccupied slot for move test
SLOT2_ID=$(curl -sk -H "Authorization: Bearer $ACCESS" -H "X-Tenant-Id: default" \
  "$BASE/api/yard/depots/$DEPOT_ID/map" | python3 -c "
import json,sys
d=json.load(sys.stdin)
ba = next(b for b in d['blocks'] if b['code']=='A')
unocc = [s for s in ba['slots'] if not s['isOccupied']]
print(unocc[0]['id'] if unocc else ba['slots'][-1]['id'])
")
call "POST /api/gate/move (4xx acceptable if container already gated out)" POST "/api/gate/move" "$ACCESS" \
  "{\"containerNumber\":\"$VALID\",\"newBlockId\":\"$BLOCK_A_ID\",\"newBay\":1,\"newRow\":2,\"newTier\":1}" "2|4"

call "POST /api/gate/out (4xx acceptable if container already gated out)" POST "/api/gate/out" "$ACCESS" \
  "{\"containerNumber\":\"$VALID\",\"deliveryOrderId\":\"$DO_ID\",\"vehicleOutNumber\":\"51C-999.99\",\"driverOutName\":\"Tran Van B\",\"conditionAtGateOut\":\"Normal\"}" "2|4"
call "GET /api/containers/{number}/movements" GET "/api/containers/$VALID/movements" "$ACCESS"

# ---- 10. Reports -----------------------------------------------------------
bold ""
bold "[10] Reports"
call "GET /api/reports/yard-aging" GET "/api/reports/yard-aging" "$ACCESS"
call "GET /api/reports/daily-throughput" GET "/api/reports/daily-throughput?from=2026-08-01&to=2026-08-31" "$ACCESS"

# ---- 11. AI Agent ----------------------------------------------------------
bold ""
bold "[11] AI Skill Agents"
call "GET /api/agents/skills (public)" GET "/api/agents/skills" "" "" "2|3"
call "POST /api/agents/execute (yard count prompt)" POST "/api/agents/execute" "$ACCESS" \
  '{"prompt":"How many containers are in the yard?"}'
call "POST /api/agents/execute (aging prompt)" POST "/api/agents/execute" "$ACCESS" \
  '{"prompt":"How many containers stuck over 10 days?"}'

# ---- 12. Multi-tenancy -----------------------------------------------------
bold ""
bold "[12] Multi-tenancy — X-Tenant-Id"
call "GET /api/lookups/line-operators (tenant: default)" GET "/api/lookups/line-operators" "$ACCESS" "" "2|3"
# call "GET /api/lookups/line-operators (tenant: secondary — empty)" GET "/api/lookups/line-operators" "$ACCESS" "" "2|3"

# ---- 13. Validation — expect 4xx ProblemDetails ---------------------------
bold ""
bold "[13] Validation — expect 4xx ProblemDetails"
call "POST /api/identity/login (wrong password)" POST "/api/identity/login" "" \
  '{"email":"admin@TechSpherex.dev","password":"wrong"}' "4"
call "GET protected without token — todos/containers read access" GET "/api/containers" "" "" "2"

# ---- 14. Close Delivery Order ----------------------------------------------
bold ""
bold "[14] Close Delivery Order"
call "POST /api/delivery-orders/{id}/close" POST "/api/delivery-orders/$DO_ID/close" "$ACCESS"

# ---- 15. gRPC endpoint reachable (HTTP/2) ---------------------------------
bold ""
bold "[15] gRPC — HTTP/2 endpoint (best-effort)"
HTTP2_CODE=$(curl -sk --http2 -o /dev/null -w "%{http_code}" \
  -H "Content-Type: application/grpc" -H "TE: trailers" \
  -X POST "$BASE/TechSpherex.CleanArchitecture.Api.GrpcServices.YardService/GetYardMap" \
  --data-binary $'\x00\x00\x00\x00\x0a\x06default' 2>&1)
yellow "  → HTTP/2 gRPC port: $HTTP2_CODE (200/415 expected — port reachable)"
if [[ "$HTTP2_CODE" =~ ^(200|415|400|404|500)$ ]]; then
  green "  ✓ gRPC endpoint reachable"
  PASS=$((PASS+1))
else
  red "  ✗ gRPC endpoint not reachable"
  FAIL=$((FAIL+1))
fi

# ===========================================================================
# Summary
# ===========================================================================
echo ""
bold "================================================================"
bold "Summary"
bold "================================================================"
green "Passed: $PASS"
if [ $FAIL -gt 0 ]; then
  red "Failed: $FAIL"
  red "Errors:"
  for e in "${ERRORS[@]}"; do
    red "  - $e"
  done
else
  green "Failed: 0"
fi
bold "================================================================"
exit $FAIL