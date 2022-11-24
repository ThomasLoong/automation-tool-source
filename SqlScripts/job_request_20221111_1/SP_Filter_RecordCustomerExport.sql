DROP PROCEDURE IF EXISTS SP_Filter_RecordCustomerExport;

DELIMITER ;;

CREATE PROCEDURE `SP_Filter_RecordCustomerExport`( 

IN dateFirstAddedFrom DATETIME,
IN dateFirstAddedTo DATETIME,

IN totalTimesExportedFrom INT,
IN totalTimesExportedTo INT,

IN dateLastExportedFrom DATETIME,
IN dateLastExportedTo DATETIME,

IN last3CampaignsUsed varchar(200),

IN dateLastOccurredFrom DATETIME,
IN dateLastOccurredTo DATETIME,

IN occurredCategories varchar(200),

IN totalOccurancePointsFrom INT,
IN totalOccurancePointsTo INT,

IN resultsCategories varchar(200),

IN totalResultsPointsFrom INT,
IN totalResultsPointsTo INT,

IN totalPointsFrom INT,
IN totalPointsTo INT,

IN exportVsPointsPercentageFrom INT,
IN exportVsPointsPercentageTo INT,

IN exportVsPointsExceptions varchar(200),

IN exportVsPointsNumberFrom INT,
IN exportVsPointsNumberTo INT,

IN sortBy varchar(200),
IN sortDirection varchar(200),

IN exportLimit INT,
in exportOffset INT
)
BEGIN
    select distinct (lm.CustomerMobileNo), ""
    from RecordCustomerExport lm
    where         
	        (if(totalTimesExportedFrom is null, 1, lm.DateExported >= totalTimesExportedFrom))
	    and (if(totalTimesExportedFrom is null, 1, lm.DateExported <= totalTimesExportedFrom))	    
	    
	    ORDER BY
    CASE WHEN sortDirection = 'asc' THEN
        CASE 
           WHEN sortBy = 'DateFirstAdded' THEN DateExported           
           ELSE ID 
        END
    END ASC
    , CASE WHEN sortDirection = 'desc' THEN
        CASE 
           WHEN sortBy = 'DateFirstAdded' THEN DateExported          
           ELSE ID 
        END
    END DESC				
	   LIMIT  exportOffset, exportLimit
   ;
END ;;