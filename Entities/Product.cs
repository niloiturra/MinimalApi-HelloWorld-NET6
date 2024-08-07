namespace MinimalApiDemo.Entities
{
    public class Product
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public double? Price { get; set; }
        public decimal? Amount { get; set; }
        public bool Active { get; set; }
        public bool Teste { get; set; }
    }
}
