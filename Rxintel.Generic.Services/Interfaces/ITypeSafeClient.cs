using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Rxintel.Generic.Services.TypeSafeAI;

namespace Rxintel.Generic.Services.Interfaces
{
    public interface ITypeSafeClient
    {
        Task<bool> AreSigsEquivalentAsync(string? mmSig, string? erxSig, IEnumerable<string?>? knownPii = null, CancellationToken cancellationToken = default);
        bool AreSigsEquivalent(string? mmSig, string? erxSig, IEnumerable<string?>? knownPii = null);
        Task<TypeSafeSigEvaluationResult> EvaluateSigDetailedAsync(string? mmSig, string? erxSig, IEnumerable<string?>? knownPii = null, CancellationToken cancellationToken = default);
    }
}
