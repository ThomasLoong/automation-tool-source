﻿using IntranetApi.Enum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IntranetApi.Models
{
    public class StaffRecord : BaseAuditEntity
    {
        public int EmployeeId { get; set; }
        public int DepartmentId { get; set; }

        [MaxLength(200)]
        public string? OtherDepartment { get; set; }

        public StaffRecordType RecordType { get; set; }
        public StaffRecordDetailType RecordDetailType { get; set; }
        public int LateAmount { get; set; }

        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int NumberOfDays { get; set; }
        public int NumberOfHours { get; set; }
        public decimal Fine { get; set; }

        [MaxLength(200)]
        public string? Remarks { get; set; }

        public virtual ICollection<StaffRecordDocument> StaffRecordDocuments { get; set; }= new HashSet<StaffRecordDocument>();

        [ForeignKey(nameof(EmployeeId))]
        public virtual User Employee { get; set; }
    }
}