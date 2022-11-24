-- use devdb;
create table AdminScore(
   ScoreID INT NOT NULL AUTO_INCREMENT,
   ScoreCategory VARCHAR(200),
   ScoreTitle VARCHAR(200),
   Points INT NOT NULL,
   Status INT NOT null,
   LastUpdatedBy INT,
   LastUpdatedON DATETIME,
   INDEX(ScoreCategory),
   INDEX(ScoreTitle),
   PRIMARY KEY ( ScoreID )
);

-- create INDEX ScoreCatergory ON AdminScore (ScoreCatergory);
-- DROP INDEX ScoreCatergory ON AdminScore;
-- SHOW INDEX FROM AdminScore
-- ALTER TABLE AdminScore CHANGE ScoreCatergory ScoreCategory VARCHAR(200);

ALTER TABLE Customer
ADD COLUMN Source VARCHAR(100) after DateFirstAdded;

ALTER TABLE customerscore 
ADD COLUMN Source VARCHAR(100) after CustomerScoreID;

ALTER TABLE leadmanagementreport  
ADD COLUMN Source VARCHAR(100) after ID;

ALTER TABLE ImportDataHistory 
ADD COLUMN Source VARCHAR(100) after ImportTime;
CREATE INDEX Source ON ImportDataHistory (Source);

create table AdminCampaign(
   CampaignID INT NOT NULL AUTO_INCREMENT,
   CampaignName VARCHAR(200),
   CampaignDate DATETIME,
   Status INT NOT null,
   LastUpdatedBy INT,
   LastUpdatedON DATETIME,
   INDEX(CampaignName),
   PRIMARY KEY ( CampaignID )
);


create table Customer(
   CustomerID INT NOT NULL AUTO_INCREMENT,
   DateFirstAdded DATETIME NOT NULL,
   CustomerMobileNo BigInt NOT NULL,
   Status INT NOT null,
   LastUpdatedBy INT,
   LastUpdatedON DATETIME, 
   UNIQUE INDEX (CustomerMobileNo),
   CONSTRAINT UNIQUE (CustomerMobileNo),
   PRIMARY KEY ( CustomerID )
);


create table CustomerScore(
   CustomerScoreID INT NOT NULL AUTO_INCREMENT,
   CustomerMobileNo BigInt NOT NULL,
   ScoreID INT NOT NULL,
   DateOccurred DATETIME NOT NULL,
   Status INT NOT null,
   LastUpdatedBy INT,
   LastUpdatedON DATETIME,
   INDEX(CustomerMobileNo),
   PRIMARY KEY ( CustomerScoreID )
);

create table RecordCustomerExport(
   ID INT NOT NULL AUTO_INCREMENT,
   CustomerMobileNo BigInt NOT NULL,
   CampaignID INT NOT NULL,
   DateExported DATETIME NOT NULL,
   Status INT NOT null,
   LastUpdatedBy INT,
   LastUpdatedON DATETIME, 
   PRIMARY KEY (ID)
);

create table ImportDataHistory(
   ID INT NOT NULL AUTO_INCREMENT,
   ImportName VARCHAR(200),
   FileName VARCHAR(300),
   ImportTime DATETIME NOT NULL,
   TotalRows INT NOT NULL,
   TotalErrorRows INT NOT NULL,
   ImportByEmail VARCHAR(200),
   INDEX(ImportName),
   PRIMARY KEY ( ID )
);

-- ALTER TABLE ImportDataHistory DROP COLUMN BackgroundProcessId;
-- ALTER TABLE ImportDataHistory DROP COLUMN BackgroundProcessStatus;

create table LeadManagementReport(
   ID INT NOT NULL AUTO_INCREMENT,
   
   -- customer
   CustomerMobileNo BigInt NOT NULL,
   DateFirstAdded DATETIME NOT NULL,
   
   -- CAMPAIGNS
   TotalTimesExported INT NOT null DEFAULT 0,
   DateLastExported DATETIME,   
   LastUsedCampaignId INT,
   SecondLastUsedCampaignId INT,
   ThirdLastUsedCampaignId INT,
   
   -- OCCURANCE (INDICATORS)
   DateLastOccurred DATETIME,   
   OccuranceTotalFirstScore INT NOT null  DEFAULT 0,
   OccuranceTotalSecondScore INT NOT null  DEFAULT 0,
   OccuranceTotalThirdScore INT NOT null  DEFAULT 0,
   OccuranceTotalFourthScore INT NOT null  DEFAULT 0,
   OccuranceTotalFifthScore INT NOT null  DEFAULT 0,
   TotalOccurancePoints INT NOT null  DEFAULT 0,
   
   -- RESULTS
   ResultsTotalFirstScore INT NOT null  DEFAULT 0,
   ResultsTotalSecondScore INT NOT null  DEFAULT 0,
   ResultsTotalThirdScore INT NOT null  DEFAULT 0,
   ResultsTotalFourthScore INT NOT null  DEFAULT 0,
   TotalResultsPoints INT NOT null  DEFAULT 0,
   
   -- ANALYSIS
   TotalPoints INT NOT null  DEFAULT 0,
   ExportVsPointsPercentage VARCHAR(10) NOT null  DEFAULT '',
   ExportVsPointsNumber INT NOT null  DEFAULT 0,
   UNIQUE INDEX(CustomerMobileNo),
   CONSTRAINT UNIQUE (CustomerMobileNo),
   PRIMARY KEY ( ID )
);

create table CleanDataHistory(
   ID INT NOT NULL AUTO_INCREMENT,   
   FileName VARCHAR(200),
   CleanTime DATETIME NOT NULL,
   Source VARCHAR(100),
   TotalRows INT NOT NULL,
   TotalInvalidNumbers INT NOT NULL,
   TotalDuplicateNumbersWithSystem INT NOT NULL,
   TotalDuplicateNumbersInFile INT NOT NULL,
   INDEX(FileName),
   INDEX(Source),
   PRIMARY KEY ( ID )
);

create table custom_mobile 
(
	id int auto_increment primary key,
    mobile_no varchar(100),
    status tinyint default 1, 
    created_at datetime default now(),
	updated_at datetime 
)
