using LearnQ.Application.Common.Models;
using LearnQ.Application.Features.Onboarding;

namespace LearnQ.Application.Common.Interfaces;

public interface IOnboardingApi
{
    Task<Result<OnboardingStatusResponse>> GetStatusAsync(CancellationToken ct = default);
    Task<Result> SelectRoleAsync(SelectRoleRequest request, CancellationToken ct = default);
    Task<Result> SubmitStudentAsync(StudentOnboardingRequest request, CancellationToken ct = default);
    Task<Result> SubmitTeacherAsync(TeacherOnboardingRequest request, CancellationToken ct = default);
}
