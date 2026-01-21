using System.Text;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;

namespace Nagaira.Ecommerce.Api.Controllers;

[ApiController]
public class SitemapController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;

    public SitemapController(IUnitOfWork unitOfWork, IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
    }

    [HttpGet("/sitemap.xml")]
    public async Task<IActionResult> GetIndex()
    {
        var baseUrl = ResolveBaseUrl();
        var xml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<sitemapindex xmlns=""http://www.sitemaps.org/schemas/sitemap/0.9"">
  <sitemap>
    <loc>{baseUrl}/sitemap-static.xml</loc>
  </sitemap>
  <sitemap>
    <loc>{baseUrl}/sitemap-products.xml</loc>
  </sitemap>
  <sitemap>
    <loc>{baseUrl}/sitemap-categories.xml</loc>
  </sitemap>
</sitemapindex>";

        return Content(xml, "application/xml", Encoding.UTF8);
    }

    [HttpGet("/sitemap-static.xml")]
    public IActionResult GetStatic()
    {
        var baseUrl = ResolveBaseUrl();
        var xml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<urlset xmlns=""http://www.sitemaps.org/schemas/sitemap/0.9"">
  <url>
    <loc>{baseUrl}/</loc>
    <changefreq>daily</changefreq>
    <priority>1.0</priority>
  </url>
  <url>
    <loc>{baseUrl}/products</loc>
    <changefreq>daily</changefreq>
    <priority>0.6</priority>
  </url>
  <url>
    <loc>{baseUrl}/categories</loc>
    <changefreq>weekly</changefreq>
    <priority>0.5</priority>
  </url>
</urlset>";

        return Content(xml, "application/xml", Encoding.UTF8);
    }

    [HttpGet("/sitemap-products.xml")]
    public async Task<IActionResult> GetProducts()
    {
        var baseUrl = ResolveBaseUrl();
        var products = await _unitOfWork.Repository<Product>()
            .FindAsync(p => p.IsActive && !p.IsDeleted);

        var builder = new StringBuilder();
        builder.Append(@"<?xml version=""1.0"" encoding=""UTF-8""?>
<urlset xmlns=""http://www.sitemaps.org/schemas/sitemap/0.9"">");

        foreach (var product in products.OrderBy(p => p.UpdatedAt ?? p.CreatedAt))
        {
            builder.Append($@"
  <url>
    <loc>{baseUrl}/p/{product.Slug}</loc>
    <lastmod>{(product.UpdatedAt ?? product.CreatedAt):yyyy-MM-dd}</lastmod>
    <changefreq>weekly</changefreq>
    <priority>0.8</priority>
  </url>");
        }

        builder.Append("\n</urlset>");
        return Content(builder.ToString(), "application/xml", Encoding.UTF8);
    }

    [HttpGet("/sitemap-categories.xml")]
    public async Task<IActionResult> GetCategories()
    {
        var baseUrl = ResolveBaseUrl();
        var categories = await _unitOfWork.Repository<Category>()
            .FindAsync(c => c.IsActive && !c.IsDeleted);

        var builder = new StringBuilder();
        builder.Append(@"<?xml version=""1.0"" encoding=""UTF-8""?>
<urlset xmlns=""http://www.sitemaps.org/schemas/sitemap/0.9"">");

        foreach (var category in categories.OrderBy(c => c.UpdatedAt ?? c.CreatedAt))
        {
            builder.Append($@"
  <url>
    <loc>{baseUrl}/c/{category.Slug}</loc>
    <lastmod>{(category.UpdatedAt ?? category.CreatedAt):yyyy-MM-dd}</lastmod>
    <changefreq>weekly</changefreq>
    <priority>0.7</priority>
  </url>");
        }

        builder.Append("\n</urlset>");
        return Content(builder.ToString(), "application/xml", Encoding.UTF8);
    }

    private string ResolveBaseUrl()
    {
        var configBase = _configuration["Seo:BaseUrl"];
        if (!string.IsNullOrWhiteSpace(configBase))
        {
            return configBase.TrimEnd('/');
        }

        return $"{Request.Scheme}://{Request.Host}";
    }
}
