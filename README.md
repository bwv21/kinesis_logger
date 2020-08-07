# Kinesis Logger

[Kinesis](https://aws.amazon.com/ko/kinesis) 를 이용한 로그 수집기

## 구조

![arch](./doc/img/arch.png)

## 요약

+ JSON
  + 로그 스키마가 존재하지 않는다
  + 로그는 JSON 형태로 가공한다
  + 데이터(Record, Row)의 단위는 줄바꿈(\n)으로 한다
+ [Logger](/src/KLoggerSuite/KLogger/)
  + 현재 C# 으로만 구현되어 있다
  + 백그라운드 스레드에서 로그를 묶어 Kinesis 로 보낸다
    + 개별로 보내는 것보다 배치로 보내는 것이 성능에 도움이 된다
    + [Batch API](https://docs.aws.amazon.com/ko_kr/kinesis/latest/APIReference/API_PutRecords.html) 를 사용한다
  + 샤드 개수를 조회하여 처리할 수 있는 만큼만 전송한다
    + 테스트 결과, 처리량 이상을 보내서 재전송하는 것보다 조절해서 보내는 쪽의 성능이 더 좋았다
  + 로그가 일정 크기 이상이면 압축한다
    + 압축 기능을 사용하려면 압축을 해제할 [Preprocessor](/src/KLoggerSuite/klogger_preprocessor)가 필요하다
  + 통신 오류나 용량 초과와 같은 오류가 발생했을 때는 일정 횟수만 재전송을 시도한다
    + 재시도 횟수를 초과하면 로그를 버린다
  + 로그 처리의 최종 성공 또는 실패를 콜백 함수로 알린다
    + 성공 또는 실패한 로그의 알림만 받을 수 있다
+ [Preprocessor Lambda](/src/KLoggerSuite/klogger_preprocessor)
  + 필수가 아니고 로거의 압축을 사용하는 경우에 필요하다
  + Firehose 에서 Record transformation 을 Enable 하고 전처리 람다를 지정한다
  + 들어온 로그가 압축되어 있으면 Firehose 로 넘기기 전에 압축을 푼다
  + 이후 다른 전처리 로직을 넣을 수 있다
+ [Merge Lambda](/src/KLoggerSuite/klogger_merge_s3)
  + 필수가 아니다
  + Firehose S3 compression 사용을 고려한다
  + S3 에 있는 파일들이 작게 쪼개져 있으면 Athena 와 같은 시스템에서 효율이 떨어질 수 있다
  + S3 에 있는 파일들을 묶어서 큰 파일로 만든다
    + 5MB 이하의 파일은 다운로드 후 병합하여 업로드한다
    + 5MB 이상의 파일은 [멀티파트 업로드](https://docs.aws.amazon.com/ko_kr/AmazonS3/latest/dev/mpuoverview.html)를 사용하여 S3 상에서 빠르게 병합한다
  + CloudWatch 로 주기적으로 호출한다
    + 람다를 여러 번 호출해도 무해하게 구현한다
