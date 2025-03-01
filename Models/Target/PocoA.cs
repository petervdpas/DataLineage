using System;

namespace Models.Target
{
    public class PocoA
    {
        public required string Bk { get; set; }
        public string? NamedCode { get; set; }
        public DateOnly Date { get; set; }
    }
}