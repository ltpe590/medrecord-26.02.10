using Core.DTOs;
using Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Core.Services
{
    public class UserService : IUserService
    {
        private readonly IApiService _api;
        private readonly ILogger<UserService> _logger;
        private readonly IUserMappingService _mapper;
        private readonly IAuthSession _authSession;

        public UserService(IApiService api, IUserMappingService mapper, ILogger<UserService> logger, IAuthSession authSession)
        {
            _api = api;
            _mapper = mapper;
            _logger = logger;
            _authSession = authSession;
        }

        public async Task<string> LoginAsync(string username, string password, string baseUrl)
        {
            _logger.LogDebug(
                "Login request started. Endpoint: api/auth/login, User: {Username}",
                username);

            try
            {
                var endpoint = "api/auth/login";
                var request = new { Username = username, Password = password };

                var response = await _api.PostAsync<object, AuthResponse>(endpoint, request);
                if (response == null)
                {
                    _logger.LogError("Login failed: Received null response from API");
                    throw new InvalidOperationException("Login failed: No response received from server");
                }

                var hasToken = !string.IsNullOrWhiteSpace(response.Token);

                if (hasToken)
                {
                    _authSession.SetToken(response.Token);
                }

                _logger.LogInformation(
                    "Login completed. User: {Username}, TokenReceived: {HasToken}",
                    username,
                    hasToken);

                return response.Token;
            }
            catch (HttpRequestException httpEx)
            {
                var statusCode = httpEx.StatusCode?.ToString() ?? "Unknown";

                _logger.LogError(
                    httpEx,
                    "HTTP error during login. User: {Username}, StatusCode: {StatusCode}",
                    username,
                    statusCode);

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Unexpected error during login. User: {Username}",
                    username);

                throw;
            }
        }


        public async Task<List<PatientDto>> GetAllPatientsAsync(string baseUrl, string token)
        {
            _api.SetAuthToken(token);
            var patients = await _api.GetAsync<List<PatientDto>>($"{baseUrl}/api/Patients");
            return patients ?? new List<PatientDto>();
        }

        public async Task CreatePatientAsync(PatientCreateDto patient, string baseUrl, string token)
        {
            _api.SetAuthToken(token);
            await _api.PostAsync<PatientCreateDto, object>($"{baseUrl}/api/Patients", patient);
        }

        public async Task UpdatePatientAsync(PatientUpdateDto dto, string baseUrl, string token)
        {
            _api.SetAuthToken(token);
            await _api.PutAsync<PatientUpdateDto>($"{baseUrl}/api/patients", dto);
        }

        public async Task DeletePatientAsync(PatientDeleteDto dto, string baseUrl, string token)
        {
            _api.SetAuthToken(token);
            await _api.DeleteAsync($"{baseUrl}/api/patients/{dto.PatientId}");
        }

        public async Task<List<VisitDto>> GetVisitsByPatientAsync(int patientId, string baseUrl, string token)
        {
            _api.SetAuthToken(token);
            var visits = await _api.GetAsync<List<VisitDto>>($"{baseUrl}/api/Visits/patient/{patientId}");
            return visits ?? new List<VisitDto>();
        }

        public async Task SaveVisitAsync(VisitDto visit, string baseUrl, string token)
        {
            _api.SetAuthToken(token);
            await _api.PostAsync<VisitDto, object>($"{baseUrl}/api/Visits", visit);
        }

        public async Task<List<TestCatalogDto>> GetTestCatalogAsync(string baseUrl, string token)
        {
            _api.SetAuthToken(token);
            var tests = await _api.GetAsync<List<TestCatalogDto>>($"{baseUrl}/api/TestCatalogs");
            return tests ?? new List<TestCatalogDto>();
        }

        public async Task<List<DrugCatalogDto>> GetDrugCatalogAsync(string baseUrl, string token)
        {
            _api.SetAuthToken(token);
            var drugs = await _api.GetAsync<List<DrugCatalogDto>>($"{baseUrl}/api/DrugCatalogs");
            return drugs ?? new List<DrugCatalogDto>();
        }

        public async Task<PatientDto?> GetPatientByIdAsync(int patientId, string authToken, string apiBaseUrl)
        {
            _api.SetAuthToken(authToken);
            return await _api.GetAsync<PatientDto>($"{apiBaseUrl}/api/Patients/{patientId}");
        }

        public async Task SaveLabResultAsync(LabResultCreateDto dto, string baseUrl, string token)
        {
            _api.SetAuthToken(token);
            await _api.PostAsync<LabResultCreateDto, object>($"{baseUrl}/api/LabResults", dto);
        }

        public async Task SavePrescriptionAsync(PrescriptionCreateDto dto, string baseUrl, string token)
        {
            _api.SetAuthToken(token);
            await _api.PostAsync<PrescriptionCreateDto, object>($"{baseUrl}/api/Prescriptions", dto);
        }

        public async Task<List<PrescriptionDto>> GetPrescriptionsByVisitAsync(int visitId, string baseUrl, string token)
        {
            _api.SetAuthToken(token);
            var prescriptions = await _api.GetAsync<List<PrescriptionDto>>($"{baseUrl}/api/Prescriptions/visit/{visitId}");
            return prescriptions ?? new List<PrescriptionDto>();
        }

        public async Task DeletePrescriptionsByVisitAsync(int visitId, string baseUrl, string token)
        {
            _api.SetAuthToken(token);
            await _api.DeleteAsync($"{baseUrl}/api/Prescriptions/visit/{visitId}");
        }
    }
}
