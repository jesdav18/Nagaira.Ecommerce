using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Application.Interfaces;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;

namespace Nagaira.Ecommerce.Application.Services;

public class OfferService : IOfferService
{
    private readonly IUnitOfWork _unitOfWork;

    public OfferService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<OfferDto>> GetAllOffersAsync()
    {
        var offers = await _unitOfWork.Offers.GetAllAsync();
        return offers.Select(MapToDto);
    }

    public async Task<OfferDto?> GetOfferByIdAsync(Guid id)
    {
        var offer = await _unitOfWork.Offers.GetByIdAsync(id);
        return offer != null ? MapToDto(offer) : null;
    }

    public async Task<IEnumerable<OfferDto>> GetActiveOffersAsync()
    {
        var offers = await _unitOfWork.Offers.GetActiveOffersAsync(DateTime.UtcNow);
        return offers.Select(MapToDto);
    }

    public async Task<OfferDto> CreateOfferAsync(CreateOfferDto dto, Guid userId)
    {
        if (!Enum.TryParse<OfferType>(dto.OfferType, true, out var offerType))
            throw new Exception("Invalid offer type");

        if (dto.StartDate >= dto.EndDate)
            throw new Exception("Start date must be before end date");

        var offer = new Offer
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            OfferType = offerType,
            Status = OfferStatus.Draft,
            DiscountPercentage = dto.DiscountPercentage,
            DiscountAmount = dto.DiscountAmount,
            MinPurchaseAmount = dto.MinPurchaseAmount,
            MinQuantity = dto.MinQuantity,
            MaxUsesPerCustomer = dto.MaxUsesPerCustomer,
            TotalMaxUses = dto.TotalMaxUses,
            CurrentUses = 0,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Priority = dto.Priority,
            IsActive = true,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Offers.AddAsync(offer);

        if (dto.ProductIds != null && dto.ProductIds.Any())
        {
            foreach (var productId in dto.ProductIds)
            {
                var offerProduct = new OfferProduct
                {
                    Id = Guid.NewGuid(),
                    OfferId = offer.Id,
                    ProductId = productId,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Repository<OfferProduct>().AddAsync(offerProduct);
            }
        }

        if (dto.CategoryIds != null && dto.CategoryIds.Any())
        {
            foreach (var categoryId in dto.CategoryIds)
            {
                var offerCategory = new OfferCategory
                {
                    Id = Guid.NewGuid(),
                    OfferId = offer.Id,
                    CategoryId = categoryId,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Repository<OfferCategory>().AddAsync(offerCategory);
            }
        }

        if (dto.ExcludedProductIds != null && dto.ExcludedProductIds.Any())
        {
            foreach (var productId in dto.ExcludedProductIds)
            {
                var excludedProduct = new OfferExcludedProduct
                {
                    Id = Guid.NewGuid(),
                    OfferId = offer.Id,
                    ProductId = productId,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Repository<OfferExcludedProduct>().AddAsync(excludedProduct);
            }
        }

        if (dto.ExcludedCategoryIds != null && dto.ExcludedCategoryIds.Any())
        {
            foreach (var categoryId in dto.ExcludedCategoryIds)
            {
                var excludedCategory = new OfferExcludedCategory
                {
                    Id = Guid.NewGuid(),
                    OfferId = offer.Id,
                    CategoryId = categoryId,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Repository<OfferExcludedCategory>().AddAsync(excludedCategory);
            }
        }

        if (dto.Rules != null && dto.Rules.Any())
        {
            foreach (var rule in dto.Rules)
            {
                if (string.IsNullOrWhiteSpace(rule.RuleType)) continue;
                if (rule.Value <= 0) continue;

                var offerRule = new OfferRule
                {
                    Id = Guid.NewGuid(),
                    OfferId = offer.Id,
                    RuleType = rule.RuleType.Trim().ToLowerInvariant(),
                    Value = rule.Value,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Repository<OfferRule>().AddAsync(offerRule);
            }
        }

        await _unitOfWork.SaveChangesAsync();
        return MapToDto(offer);
    }

    public async Task UpdateOfferAsync(UpdateOfferDto dto)
    {
        var offer = await _unitOfWork.Offers.GetByIdAsync(dto.Id);
        if (offer == null) throw new Exception("Offer not found");

        if (dto.StartDate >= dto.EndDate)
            throw new Exception("Start date must be before end date");

        offer.Name = dto.Name;
        offer.Description = dto.Description;
        offer.DiscountPercentage = dto.DiscountPercentage;
        offer.DiscountAmount = dto.DiscountAmount;
        offer.MinPurchaseAmount = dto.MinPurchaseAmount;
        offer.MinQuantity = dto.MinQuantity;
        offer.MaxUsesPerCustomer = dto.MaxUsesPerCustomer;
        offer.TotalMaxUses = dto.TotalMaxUses;
        offer.StartDate = dto.StartDate;
        offer.EndDate = dto.EndDate;
        offer.Priority = dto.Priority;
        offer.IsActive = dto.IsActive;
        // UpdatedAt no existe en la tabla offers

        if (!Enum.TryParse<OfferStatus>(dto.Status, true, out var status))
            throw new Exception("Invalid offer status");
        offer.Status = status;

        var existingProducts = (await _unitOfWork.Repository<OfferProduct>()
            .FindAsync(op => op.OfferId == offer.Id && !op.IsDeleted)).Cast<OfferProduct>();
        
        var existingProductIds = existingProducts.Select(ep => ep.ProductId).ToList();
        var newProductIds = dto.ProductIds ?? new List<Guid>();

        var toRemove = existingProductIds.Except(newProductIds).ToList();
        var toAdd = newProductIds.Except(existingProductIds).ToList();

        foreach (var productId in toRemove)
        {
            var offerProduct = existingProducts.FirstOrDefault(ep => ep.ProductId == productId);
            if (offerProduct != null)
            {
                await _unitOfWork.Repository<OfferProduct>().DeleteAsync(offerProduct.Id);
            }
        }

        foreach (var productId in toAdd)
        {
            var offerProduct = new OfferProduct
            {
                Id = Guid.NewGuid(),
                OfferId = offer.Id,
                ProductId = productId,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Repository<OfferProduct>().AddAsync(offerProduct);
        }

        var existingCategories = (await _unitOfWork.Repository<OfferCategory>()
            .FindAsync(oc => oc.OfferId == offer.Id && !oc.IsDeleted)).Cast<OfferCategory>();
        
        var existingCategoryIds = existingCategories.Select(ec => ec.CategoryId).ToList();
        var newCategoryIds = dto.CategoryIds ?? new List<Guid>();

        var categoriesToRemove = existingCategoryIds.Except(newCategoryIds).ToList();
        var categoriesToAdd = newCategoryIds.Except(existingCategoryIds).ToList();

        foreach (var categoryId in categoriesToRemove)
        {
            var offerCategory = existingCategories.FirstOrDefault(ec => ec.CategoryId == categoryId);
            if (offerCategory != null)
            {
                await _unitOfWork.Repository<OfferCategory>().DeleteAsync(offerCategory.Id);
            }
        }

        foreach (var categoryId in categoriesToAdd)
        {
            var offerCategory = new OfferCategory
            {
                Id = Guid.NewGuid(),
                OfferId = offer.Id,
                CategoryId = categoryId,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Repository<OfferCategory>().AddAsync(offerCategory);
        }

        var existingExcludedProducts = (await _unitOfWork.Repository<OfferExcludedProduct>()
            .FindAsync(ep => ep.OfferId == offer.Id && !ep.IsDeleted)).Cast<OfferExcludedProduct>();
        var existingExcludedProductIds = existingExcludedProducts.Select(ep => ep.ProductId).ToList();
        var newExcludedProductIds = dto.ExcludedProductIds ?? new List<Guid>();

        var excludedProductsToRemove = existingExcludedProductIds.Except(newExcludedProductIds).ToList();
        var excludedProductsToAdd = newExcludedProductIds.Except(existingExcludedProductIds).ToList();

        foreach (var productId in excludedProductsToRemove)
        {
            var excludedProduct = existingExcludedProducts.FirstOrDefault(ep => ep.ProductId == productId);
            if (excludedProduct != null)
            {
                await _unitOfWork.Repository<OfferExcludedProduct>().DeleteAsync(excludedProduct.Id);
            }
        }

        foreach (var productId in excludedProductsToAdd)
        {
            var excludedProduct = new OfferExcludedProduct
            {
                Id = Guid.NewGuid(),
                OfferId = offer.Id,
                ProductId = productId,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Repository<OfferExcludedProduct>().AddAsync(excludedProduct);
        }

        var existingExcludedCategories = (await _unitOfWork.Repository<OfferExcludedCategory>()
            .FindAsync(ec => ec.OfferId == offer.Id && !ec.IsDeleted)).Cast<OfferExcludedCategory>();
        var existingExcludedCategoryIds = existingExcludedCategories.Select(ec => ec.CategoryId).ToList();
        var newExcludedCategoryIds = dto.ExcludedCategoryIds ?? new List<Guid>();

        var excludedCategoriesToRemove = existingExcludedCategoryIds.Except(newExcludedCategoryIds).ToList();
        var excludedCategoriesToAdd = newExcludedCategoryIds.Except(existingExcludedCategoryIds).ToList();

        foreach (var categoryId in excludedCategoriesToRemove)
        {
            var excludedCategory = existingExcludedCategories.FirstOrDefault(ec => ec.CategoryId == categoryId);
            if (excludedCategory != null)
            {
                await _unitOfWork.Repository<OfferExcludedCategory>().DeleteAsync(excludedCategory.Id);
            }
        }

        foreach (var categoryId in excludedCategoriesToAdd)
        {
            var excludedCategory = new OfferExcludedCategory
            {
                Id = Guid.NewGuid(),
                OfferId = offer.Id,
                CategoryId = categoryId,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Repository<OfferExcludedCategory>().AddAsync(excludedCategory);
        }

        if (dto.Rules != null)
        {
            var existingRules = (await _unitOfWork.Repository<OfferRule>()
                .FindAsync(r => r.OfferId == offer.Id && !r.IsDeleted)).Cast<OfferRule>().ToList();

            var incomingRules = dto.Rules
                .Where(r => !string.IsNullOrWhiteSpace(r.RuleType) && r.Value > 0)
                .Select(r => new OfferRuleDto(r.RuleType.Trim().ToLowerInvariant(), r.Value))
                .ToList();

            var incomingKeys = new HashSet<string>(incomingRules.Select(r => $"{r.RuleType}|{r.Value}"));
            var existingKeys = new HashSet<string>(existingRules.Select(r => $"{r.RuleType.ToLowerInvariant()}|{r.Value}"));

            foreach (var rule in existingRules.Where(r => !incomingKeys.Contains($"{r.RuleType.ToLowerInvariant()}|{r.Value}")))
            {
                await _unitOfWork.Repository<OfferRule>().DeleteAsync(rule.Id);
            }

            foreach (var rule in incomingRules.Where(r => !existingKeys.Contains($"{r.RuleType}|{r.Value}")))
            {
                var offerRule = new OfferRule
                {
                    Id = Guid.NewGuid(),
                    OfferId = offer.Id,
                    RuleType = rule.RuleType,
                    Value = rule.Value,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Repository<OfferRule>().AddAsync(offerRule);
            }
        }

        await _unitOfWork.Offers.UpdateAsync(offer);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteOfferAsync(Guid id)
    {
        await _unitOfWork.Offers.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task ActivateOfferAsync(Guid id)
    {
        var offer = await _unitOfWork.Offers.GetByIdAsync(id);
        if (offer == null) throw new Exception("Offer not found");

        offer.Status = OfferStatus.Active;
        offer.IsActive = true;
        // UpdatedAt no existe en la tabla offers

        await _unitOfWork.Offers.UpdateAsync(offer);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeactivateOfferAsync(Guid id)
    {
        var offer = await _unitOfWork.Offers.GetByIdAsync(id);
        if (offer == null) throw new Exception("Offer not found");

        offer.Status = OfferStatus.Paused;
        offer.IsActive = false;
        // UpdatedAt no existe en la tabla offers

        await _unitOfWork.Offers.UpdateAsync(offer);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<IEnumerable<OfferApplicationDto>> GetOfferApplicationsAsync(Guid offerId)
    {
        var applications = (await _unitOfWork.Repository<OfferApplication>()
            .FindAsync(a => ((OfferApplication)a).OfferId == offerId)).Cast<OfferApplication>();
        
        return applications.Select(app => new OfferApplicationDto(
            app.Id,
            app.OfferId,
            app.Offer?.Name ?? string.Empty,
            app.OrderId,
            app.ProductId,
            app.DiscountAmount,
            app.AppliedAt
        ));
    }

    private static OfferDto MapToDto(Offer offer)
    {
        return new OfferDto(
            offer.Id,
            offer.Name,
            offer.Description,
            offer.OfferType.ToString(),
            offer.Status.ToString(),
            offer.DiscountPercentage,
            offer.DiscountAmount,
            offer.MinPurchaseAmount,
            offer.MinQuantity,
            offer.MaxUsesPerCustomer,
            offer.TotalMaxUses,
            offer.CurrentUses,
            offer.StartDate,
            offer.EndDate,
            offer.Priority,
            offer.IsActive,
            offer.Products.Where(p => !p.IsDeleted).Select(p => p.ProductId).ToList(),
            offer.Categories.Where(c => !c.IsDeleted).Select(c => c.CategoryId).ToList(),
            offer.ExcludedProducts.Where(p => !p.IsDeleted).Select(p => p.ProductId).ToList(),
            offer.ExcludedCategories.Where(c => !c.IsDeleted).Select(c => c.CategoryId).ToList(),
            offer.Rules.Where(r => !r.IsDeleted).Select(r => new OfferRuleDto(r.RuleType, r.Value)).ToList()
        );
    }
}

