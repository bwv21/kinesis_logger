# Kinesis Logger

[Kinesis](https://aws.amazon.com/ko/kinesis) 를 이용한 로그 수집기

## 구조

![arch](./doc/img/arch.png)

### Logger

[로거](/src/KLoggerSuite/KLogger/)

### Preprocess Lambda

[전처리 람다](/src/KLoggerSuite/klogger_preprocessor)

### Merge Lambda

[머지 람다](/src/KLoggerSuite/klogger_merge_s3)