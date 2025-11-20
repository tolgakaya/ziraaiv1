namespace Entities.Dtos
{
    /// <summary>
    /// Recommended product category with market size estimation
    /// </summary>
    public class RecommendedProductDto
    {
        /// <summary>
        /// Product category name (e.g., "Fungisit", "İnsektisit")
        /// </summary>
        public string ProductCategory { get; set; }

        /// <summary>
        /// Estimated market size in currency units
        /// </summary>
        public decimal EstimatedMarketSize { get; set; }
    }
}
