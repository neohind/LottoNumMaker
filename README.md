# LottoNumMaker

### 데이터베이스 구성 쿼리
사용되는 DB는 MS SQL로 MS SQL Express 등으로 설치 구성하면 된다.

#### 테이블 생성
```
CREATE TABLE [dbo].[TB_ANALY_CALC](
	[seqid] [int] IDENTITY(1,1) NOT NULL,
	[idx] [int] NULL,
	[avg_int] [int] NULL,
	[average] [real] NULL,
	[stddev_int] [int] NULL,
	[stddev] [real] NULL,
	[summary] [int] NULL,
 CONSTRAINT [PK_TB_ANALY_CALC] PRIMARY KEY CLUSTERED 
(
	[seqid] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
```
```
CREATE TABLE [dbo].[TB_ANALY_CNT](
	[seqid] [numeric](18, 0) IDENTITY(1,1) NOT NULL,
	[lvl] [tinyint] NULL,
	[b1] [tinyint] NULL,
	[b2] [tinyint] NULL,
	[b3] [tinyint] NULL,
	[b4] [tinyint] NULL,
	[b5] [tinyint] NULL,
	[b6] [tinyint] NULL,
	[cnt] [int] NULL,
	[idx] [int] NULL,
 CONSTRAINT [PK_TB_ANALY_CNT] PRIMARY KEY CLUSTERED 
(
	[seqid] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
```

```
CREATE TABLE [dbo].[TB_BALLRESULTS](
	[seqid] [int] IDENTITY(1,1) NOT NULL,
	[idx] [int] NOT NULL,
	[b1] [tinyint] NOT NULL,
	[b2] [tinyint] NOT NULL,
	[b3] [tinyint] NOT NULL,
	[b4] [tinyint] NOT NULL,
	[b5] [tinyint] NOT NULL,
	[b6] [tinyint] NOT NULL,
	[b7] [tinyint] NOT NULL,
 CONSTRAINT [PK_TB_BALLRESULTS] PRIMARY KEY CLUSTERED 
(
	[seqid] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
```

#### Connection String 처리
처음에는 소스 내에 그대로 담았는데, 변경의 편의를 위해서 ConnectionString.txt 파일 안에 Connection String을 담으면 그 내용으로 사용된다. 
이 Connection String 정보는 MS SQL 연결을 위한 Connection String이 담겨있으면 된다. 
