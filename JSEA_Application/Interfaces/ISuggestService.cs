using JSEA_Application.DTOs.Respone.Journey;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JSEA_Application.Interfaces
{
    public interface ISuggestService
    {
    
        /// <param name="journeyId">Journey đang chạy.</param>
        /// <param name="segmentId">Segment GPS vừa trigger.</param>
        /// <param name="cancellationToken"></param>
        Task<List<SuggestionResponse>> GetSuggestionsAsync(
            Guid journeyId,
            Guid segmentId,
            CancellationToken cancellationToken = default);

       
        /// <param name="suggestionId">Id của journey_suggestion cần generate insight.</param>
        /// <param name="cancellationToken"></param>
        Task<string?> GetAiInsightAsync(
            Guid suggestionId,
            CancellationToken cancellationToken = default);
    }

}
