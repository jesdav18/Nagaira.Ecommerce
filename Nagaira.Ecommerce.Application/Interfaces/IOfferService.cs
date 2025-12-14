using Nagaira.Ecommerce.Application.DTOs;

namespace Nagaira.Ecommerce.Application.Interfaces;

public interface IOfferService
{
    Task<IEnumerable<OfferDto>> GetAllOffersAsync();
    Task<OfferDto?> GetOfferByIdAsync(Guid id);
    Task<IEnumerable<OfferDto>> GetActiveOffersAsync();
    Task<OfferDto> CreateOfferAsync(CreateOfferDto dto, Guid userId);
    Task UpdateOfferAsync(UpdateOfferDto dto);
    Task DeleteOfferAsync(Guid id);
    Task ActivateOfferAsync(Guid id);
    Task DeactivateOfferAsync(Guid id);
    Task<IEnumerable<OfferApplicationDto>> GetOfferApplicationsAsync(Guid offerId);
}

