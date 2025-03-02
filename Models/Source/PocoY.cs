using System;

namespace Models.Source
{
    public class PocoY
    {
        public int Id { get; set; }
        public int PocoXId { get; set; } 
        public string? Code { get; set; }
        public bool IsActive { get; set; }
        public DateOnly PocoYDate { get; set; }
    }
}