using LearnQ.Application.Common.Enums;
using LearnQ.Application.Common.Interfaces;
using LearnQ.Application.Common.Models;
using LearnQ.Application.Features.Onboarding;
using LearnQ.Infrastructure.Api.Auth;

namespace LearnQ.Infrastructure.Api.Onboarding;

internal sealed class StubOnboardingApi : IOnboardingApi
{
    private static UserRole? _currentRole;
    private static bool _completed;

    public async Task<Result<OnboardingStatusResponse>> GetStatusAsync(CancellationToken ct = default)
    {
        await Task.Delay(100, ct);
        return Result<OnboardingStatusResponse>.Success(new OnboardingStatusResponse(_currentRole, _completed));
    }

    public async Task<Result> SelectRoleAsync(SelectRoleRequest request, CancellationToken ct = default)
    {
        await Task.Delay(100, ct);
        _currentRole = request.Role;
        StubAuthApi.SetRole(request.Role);
        return Result.Success();
    }

    public async Task<Result> SubmitStudentAsync(StudentOnboardingRequest request, CancellationToken ct = default)
    {
        await Task.Delay(150, ct);
        _currentRole = UserRole.Student;
        _completed = true;
        StubAuthApi.SetOnboardingCompleted(true, UserRole.Student);
        return Result.Success();
    }

    public async Task<Result> SubmitTeacherAsync(TeacherOnboardingRequest request, CancellationToken ct = default)
    {
        await Task.Delay(150, ct);
        _currentRole = UserRole.Teacher;
        _completed = true;
        StubAuthApi.SetOnboardingCompleted(true, UserRole.Teacher);
        return Result.Success();
    }
}
