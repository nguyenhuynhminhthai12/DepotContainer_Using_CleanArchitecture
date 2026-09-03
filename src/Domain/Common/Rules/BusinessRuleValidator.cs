namespace TechSpherex.CleanArchitecture.Domain.Common.Rules;

    /// <summary>
    /// Lớp tiện ích tĩnh (static helper) để kiểm tra quy tắc ở tầng miền.
    /// Các thực thể gọi <c>BusinessRuleValidator.CheckRule(rule)</c> trong các phương thức của chúng.
    /// </summary>
    public static class BusinessRuleValidator
    {
        /// <summary>
        /// Đánh giá một quy tắc và ném ra <see cref="BusinessRuleException"/> nếu quy tắc bị vi phạm.
        /// </summary>
        /// <param name="rule">Quy tắc cần kiểm tra.</param>
        /// <exception cref="BusinessRuleException">Nếu <paramref name="rule"/>.IsBroken() trả về true.</exception>
        public static void CheckRule(IBusinessRule rule)
        {
            if (rule.IsBroken())
            {
                throw new BusinessRuleException(rule);
            }
        }

        /// <summary>
        /// Đánh giá nhiều quy tắc theo thứ tự ưu tiên và ném ngoại lệ khi gặp quy tắc đầu tiên bị vi phạm.
        /// </summary>
        /// <param name="rules">Danh sách các quy tắc cần kiểm tra.</param>
        /// <exception cref="BusinessRuleException">Nếu bất kỳ quy tắc nào bị vi phạm.</exception>
        public static void CheckRules(params IBusinessRule[] rules)
        {
            foreach (var rule in rules.OrderBy(r => r.Priority))
            {
                CheckRule(rule);
            }
        }
    }
