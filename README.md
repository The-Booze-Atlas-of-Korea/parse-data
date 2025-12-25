# parse-data

Dotnet 9 콘솔 도구 모음으로, 음식점 관련 데이터셋을 Qdrant 및 MySQL/Elasticsearch로 적재하거나 상호 매칭하는 워크플로를 제공합니다. 각 도구는 서로 독립 실행 가능하지만, `upsert-data` → `find-and-make` → `insert_to_db`/`db_json_to_es_ndjson` 순으로 연결하면 통합 파이프라인을 구성할 수 있습니다.

## 주요 폴더 구조
- `data/` – 기본 입력 데이터 (`음식점-데이터셋.json`, `일반음식점.xml`). 빌드 시 개별 프로젝트의 출력 폴더로 복사됩니다.
- `upsert-data/` – 음식점 JSON을 읽어 OpenAI 임베딩을 만든 뒤 Qdrant 컬렉션(`barsDataset`)으로 업서트합니다.
- `find-and-make/` – 공공 XML 데이터(`일반음식점.xml`)를 파싱해 Qdrant의 `barsDataset`과 매칭하고 결과 JSON을 생성합니다.
- `insert_to_db/` – 매칭 결과 JSON(`bars.json` 등)을 MySQL `bars` 테이블로 대량 적재합니다.
- `db_json_to_es_ndjson/` – JSON 결과를 Elasticsearch 벌크 업서트용 NDJSON(`bars_seed.ndjson`)으로 변환합니다.
- `qdrant_compose.yaml` / `qdrant_data/` – 로컬 Qdrant 실행을 위한 Docker Compose 설정과 영구 스토리지 마운트 디렉터리입니다.
- `parse-data.sln` – 모든 콘솔 프로젝트를 포함하는 Visual Studio 솔루션입니다.

## 사전 준비
- **.NET SDK 9.0**
- **Docker & Docker Compose** – `qdrant_compose.yaml`로 Qdrant(포트 6333/6334)를 실행합니다.
- **환경 변수** `OPENAI_API_KEY` – OpenAI Embedding API 키. 코드에서는 엔드포인트를 `https://gms.ssafy.io/gmsapi/api.openai.com/v1`로 설정합니다.
- (옵션) **MySQL** – `insert_to_db` 실행 시 접속 가능한 MySQL 인스턴스와 `bars` 테이블 스키마가 필요합니다.

## 실행 방법
### 1) Qdrant 기동
```bash
docker compose -f qdrant_compose.yaml up -d
```
- 데이터는 `qdrant_data/`에 저장됩니다.

### 2) Qdrant에 임베딩 업서트 (`upsert-data`)
```bash
OPENAI_API_KEY=... dotnet run --project upsert-data
```
- 입력: `data/음식점-데이터셋.json`
- 동작: 각 레코드를 `text-embedding-3-small`로 임베딩 후 `barsDataset` 컬렉션(벡터 크기 1536)에 업서트합니다. 기존 컬렉션이 있으면 삭제 후 재생성합니다.

### 3) 공공 XML과 Qdrant 매칭 (`find-and-make`)
```bash
OPENAI_API_KEY=... dotnet run --project find-and-make
```
- 입력: `data/일반음식점.xml`
- 동작: XML 행별 텍스트를 임베딩해 Qdrant `barsDataset`에서 유사도 검색 후 주소·거리 기준으로 최적 매칭을 선택합니다.
- 출력: `find-and-make/bin/Debug/net9.0/output/matched_results.json` (빌드 설정에 따라 경로 변동 가능)

### 4) 매칭 JSON을 MySQL에 적재 (`insert_to_db`)
```bash
dotnet run --project insert_to_db -- <bars.json 경로> "Server=localhost;Port=33060;Database=sulmap;Uid=ssafy;Pwd=ssafy;CharSet=utf8mb4;"
```
- 입력: `find-and-make` 결과 JSON(기본 키 `Name`, `Adress`, 좌표 X/Y 등)
- 동작: EPSG:5174 좌표를 WGS84로 변환해 `bars` 테이블로 배치 삽입합니다. 기본 배치 크기는 2,000건입니다.

### 5) Elasticsearch NDJSON 생성 (`db_json_to_es_ndjson`)
```bash
dotnet run --project db_json_to_es_ndjson -- <bars.json 경로>
```
- 입력: `bars.json` (또는 동일 스키마의 JSON)
- 출력: `db_json_to_es_ndjson/bin/Debug/net9.0/output/bars_seed.ndjson` – Elasticsearch 벌크 API에서 바로 사용할 수 있는 NDJSON입니다.

## 추가 팁
- 각 콘솔 앱의 기본 입력 경로는 프로젝트 출력 폴더 기준이므로, 커스텀 입력을 쓰려면 커맨드라인 인자로 절대/상대 경로를 전달하세요.
- Qdrant, OpenAI 호출이 필요한 워크플로는 네트워크 연결과 유효한 API 키가 있어야 정상 동작합니다.
