using Vakantiepark.Shared.DTOs;

namespace Vakantiepark.Application.Validations.Paging
{
    public static class PagingValidation
    {
        public static void ValidatePaging(this PagedRequest request)
        {
            if (request.Page < 1)
                throw new ArgumentException("Het paginanummer moet 1 of hoger zijn.");

            if (request.PageSize < 1 || request.PageSize > PagedRequest.MaximumPageSize)
                throw new ArgumentException($"Kies tussen 1 en {PagedRequest.MaximumPageSize} regels per pagina.");
        }
    }
}
