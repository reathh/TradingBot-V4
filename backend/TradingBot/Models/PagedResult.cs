using System.Collections.Generic;

namespace TradingBot.Models;

/// <summary>
/// Generic class for paginated API results
/// </summary>
/// <typeparam name="T">Type of items in the collection</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int Page { get; set; }
    
    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; }
    
    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }
    
    /// <summary>
    /// Total number of items across all pages
    /// </summary>
    public int TotalItems { get; set; }
    
    /// <summary>
    /// Collection of items for the current page
    /// </summary>
    public required IEnumerable<T> Items { get; set; }
} 