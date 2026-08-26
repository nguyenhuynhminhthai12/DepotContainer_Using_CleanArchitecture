// ===========================================================================
// Jenkinsfile — TechSpherex Container Depot
// ===========================================================================
// Pipeline:  restore → build → test (+ coverage gate ≥ 80%)
//            → docker build (JIT + Native AOT) → docker push DockerHub
// ===========================================================================
// Required Jenkins plugins:
//   - Pipeline (workflow-aggregator)
//   - Docker Pipeline
//   - MSBuild Plugin (if running .NET Framework targets — not needed for .NET 10)
//   - xUnit Plugin (publishes TRX reports)
//   - Cobertura Plugin (publishes coverage report)
//   - Slack Notification (optional — uncomment stage to enable)
// ===========================================================================

pipeline {
    agent {
        label 'docker'    // agent must have Docker + .NET 10 SDK installed
    }

    options {
        timestamps()
        timeout(time: 30, unit: 'MINUTES')
        disableConcurrentBuilds()
        ansiColor('xterm')
        buildDiscarder(logRotator(numToKeepStr: '20', artifactNumToKeepStr: '10'))
    }

    environment {
        DOTNET_CLI_TELEMETRY_OPTOUT = '1'
        DOTNET_NOLOGO               = '1'
        DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'

        // Solution-wide properties
        SOLUTION           = 'src/TechSpherex.CleanArchitecture.slnx'
        COVERAGE_THRESHOLD = '80'     // Application layer must stay ≥ 80%
        COPY_LOCAL_LOCK    = 'true'   // fix for test runtime to find transitive deps

        // Docker
        DOCKERHUB_REPO     = 'techspherex/depot-api'
        DOCKERHUB_CREDS    = 'dockerhub-techspherex'   // Jenkins credentials ID
        IMAGE_TAG          = "${env.BUILD_NUMBER}-${env.GIT_COMMIT?.take(7)}"

        // .NET publish outputs (used by Dockerfile)
        PUBLISH_DIR        = 'src/Api/bin/Release/net10.0/publish'
    }

    stages {

        // ---------------------------------------------------------------
        // 1. Restore NuGet packages
        // ---------------------------------------------------------------
        stage('Restore') {
            steps {
                echo "==> Restoring ${SOLUTION}"
                sh '''
                    dotnet restore ${SOLUTION}
                '''
            }
        }

        // ---------------------------------------------------------------
        // 2. Build (Release)
        // ---------------------------------------------------------------
        stage('Build') {
            steps {
                echo "==> Building ${SOLUTION}"
                sh '''
                    dotnet build ${SOLUTION} \
                        --configuration Release \
                        --no-restore \
                        --maxcpucount:1 \
                        /p:CopyLocalLockFileAssemblies=${COPY_LOCAL_LOCK}
                '''
            }
        }

        // ---------------------------------------------------------------
        // 3. Unit tests + coverage gate
        // ---------------------------------------------------------------
        stage('Test') {
            steps {
                echo "==> Running tests with coverage (Application layer gate ≥ ${COVERAGE_THRESHOLD}%)"
                sh """
                    dotnet test ${SOLUTION} \
                        --configuration Release \
                        --no-restore \
                        --no-build \
                        --logger "trx;LogFileName=test-results.trx" \
                        --results-directory TestResults \
                        /p:CollectCoverage=true \
                        /p:CoverletOutput=../TestResults/coverage/ \
                        /p:CoverletOutputFormat=cobertura \
                        /p:CopyLocalLockFileAssemblies=${COPY_LOCAL_LOCK} \
                        /p:Threshold=${COVERAGE_THRESHOLD} \
                        /p:ThresholdType=line \
                        /p:ThresholdStat=total
                """
            }
            post {
                always {
                    xunit(
                        testResultsPattern: 'TestResults/**/*.trx',
                        thresholdMode: 1,
                        failIfNoTests: true
                    )
                    cobertura(
                        coberturaReportFile: 'TestResults/coverage/coverage.cobertura.xml',
                        healthyTarget: [ [columnCoverage: 80], [lineCoverage: 80], [branchCoverage: 70] ],
                        unhealthyTarget: [ [columnCoverage: 60], [lineCoverage: 60], [branchCoverage: 50] ],
                        failUnhealthy: true,
                        failUnstable: false
                    )
                }
            }
        }

        // ---------------------------------------------------------------
        // 4. Architecture tests — fail fast on dependency rule violations
        // ---------------------------------------------------------------
        stage('Architecture') {
            steps {
                echo "==> Verifying Clean Architecture dependency rules"
                sh """
                    dotnet test tests/Architecture.Tests/TechSpherex.CleanArchitecture.Architecture.Tests.csproj \
                        --configuration Release \
                        --no-restore \
                        --no-build \
                        --logger "trx;LogFileName=arch-results.trx" \
                        --results-directory TestResults/Architecture \
                        /p:CopyLocalLockFileAssemblies=${COPY_LOCAL_LOCK}
                """
            }
        }

        // ---------------------------------------------------------------
        // 5. Docker build — JIT (Alpine)
        // ---------------------------------------------------------------
        stage('Docker Build (JIT)') {
            when {
                anyOf {
                    branch 'main'
                    branch 'master'
                    branch 'release/*'
                    buildingTag()
                }
            }
            steps {
                echo "==> Building JIT image ${DOCKERHUB_REPO}:${IMAGE_TAG}-jit"
                sh """
                    docker build \
                        --tag ${DOCKERHUB_REPO}:${IMAGE_TAG}-jit \
                        --tag ${DOCKERHUB_REPO}:jit-latest \
                        --build-arg PUBLISH_AOT=false \
                        --label org.opencontainers.image.revision=${env.GIT_COMMIT} \
                        --label org.opencontainers.image.version=${IMAGE_TAG} \
                        --label org.opencontainers.image.source=${env.GIT_URL} \
                        .
                """
            }
        }

        // ---------------------------------------------------------------
        // 6. Docker build — Native AOT (Alpine, ~35 MB image)
        // ---------------------------------------------------------------
        stage('Docker Build (AOT)') {
            when {
                anyOf {
                    branch 'main'
                    branch 'master'
                    branch 'release/*'
                    buildingTag()
                }
            }
            steps {
                echo "==> Building Native AOT image ${DOCKERHUB_REPO}:${IMAGE_TAG}-aot"
                sh """
                    docker build \
                        --tag ${DOCKERHUB_REPO}:${IMAGE_TAG}-aot \
                        --tag ${DOCKERHUB_REPO}:aot-latest \
                        --build-arg PUBLISH_AOT=true \
                        --label org.opencontainers.image.revision=${env.GIT_COMMIT} \
                        --label org.opencontainers.image.version=${IMAGE_TAG} \
                        .
                """
            }
        }

        // ---------------------------------------------------------------
        // 7. Smoke test — start the container, hit /health, stop it
        // ---------------------------------------------------------------
        stage('Smoke Test') {
            when {
                anyOf {
                    branch 'main'
                    branch 'master'
                }
            }
            steps {
                echo "==> Starting container for smoke test"
                sh """
                    docker run --rm -d \
                        --name techspherex-smoke \
                        -p 18080:8080 \
                        -e ASPNETCORE_ENVIRONMENT=Production \
                        ${DOCKERHUB_REPO}:${IMAGE_TAG}-jit
                    sleep 10
                    curl -sf http://localhost:18080/health || (docker logs techspherex-smoke && exit 1)
                    docker stop techspherex-smoke
                """
            }
        }

        // ---------------------------------------------------------------
        // 8. Push to DockerHub
        // ---------------------------------------------------------------
        stage('Push') {
            when {
                anyOf {
                    branch 'main'
                    branch 'master'
                    buildingTag()
                }
            }
            steps {
                echo "==> Pushing images to DockerHub"
                withCredentials([usernamePassword(
                    credentialsId: "${DOCKERHUB_CREDS}",
                    usernameVariable: 'DOCKER_USER',
                    passwordVariable: 'DOCKER_PASS'
                )]) {
                    sh """
                        echo \${DOCKER_PASS} | docker login -u \${DOCKER_USER} --password-stdin
                        docker push ${DOCKERHUB_REPO}:${IMAGE_TAG}-jit
                        docker push ${DOCKERHUB_REPO}:${IMAGE_TAG}-aot
                        docker push ${DOCKERHUB_REPO}:jit-latest
                        docker push ${DOCKERHUB_REPO}:aot-latest
                    """
                }
            }
        }
    }

    post {
        success {
            echo "✅ Build #${env.BUILD_NUMBER} succeeded"
            // slackSend(channel: '#deployments', color: 'good',
            //          message: "✅ ${env.JOB_NAME} #${env.BUILD_NUMBER} — ${DOCKERHUB_REPO}:${IMAGE_TAG}")
        }
        failure {
            echo "❌ Build #${env.BUILD_NUMBER} failed"
            // slackSend(channel: '#deployments', color: 'danger',
            //          message: "❌ ${env.JOB_NAME} #${env.BUILD_NUMBER} failed")
        }
        always {
            cleanWs(deleteDirs: false, patterns: [[pattern: 'src/**/bin/**', type: 'INCLUDE'],
                                                 [pattern: 'src/**/obj/**', type: 'INCLUDE']])
        }
    }
}
