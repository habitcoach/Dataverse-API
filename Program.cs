using Microsoft.Identity.Client;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace dataverseapi
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            string resource = "https://orgf27b16eb.api.crm8.dynamics.com";

            var clientId = "c05a3886-f5ac-4338-8eff-58d43ed0c065";
            var redirectUri = "http://localhost";

            #region auth

            var authBuilder = PublicClientApplicationBuilder.Create(clientId)
                           .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs)
                           .WithRedirectUri(redirectUri)
                           .Build();

            var scope = resource + "/user_impersonation";
            string[] scopes = { scope };

            AuthenticationResult token =
               await authBuilder.AcquireTokenInteractive(scopes).ExecuteAsync();

            #endregion

            #region User Info


            var client = new HttpClient
            {
                BaseAddress = new Uri(resource + "/api/data/v9.2/"),
                Timeout = new TimeSpan(0, 2, 0)    // Standard two minute timeout on web service calls.
            };

            HttpRequestHeaders headers = client.DefaultRequestHeaders;
            headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
            headers.Add("OData-MaxVersion", "4.0");
            headers.Add("OData-Version", "4.0");
            headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.GetAsync("WhoAmI");

            if (response.IsSuccessStatusCode)
            {
                Guid userId = new();

                string jsonContent = await response.Content.ReadAsStringAsync();

                // Using System.Text.Json
                using (JsonDocument doc = JsonDocument.Parse(jsonContent))
                {
                    JsonElement root = doc.RootElement;
                    JsonElement userIdElement = root.GetProperty("UserId");
                    userId = userIdElement.GetGuid();
                }

                Console.WriteLine($"Your user ID is {userId}");
            }
            else
            {
                Console.WriteLine("Web API call failed");
                Console.WriteLine("Reason: " + response.ReasonPhrase);
            }

            #endregion


            #region Web API Table Info

            var responseTable = await client.GetAsync("pub01_employees");

            if (response.IsSuccessStatusCode)
            {
                string jsonContent = await responseTable.Content.ReadAsStringAsync();

                var jsonResponse = JsonConvert.DeserializeObject<JsonResponse>(jsonContent);
                var employees = new List<Employee>();

                foreach (var value in jsonResponse.Value)
                {
                    var employee = new Employee
                    {
                        Name = value.pub01_name,
                        Age = value.pub01_age,
                        EmpId = value.pub01_empid,
                        Salary = value.pub01_salary
                    };

                    employees.Add(employee);
                }

                foreach (var value in employees)
                {
                    Console.WriteLine(value.EmpId + "   " + value.Name + "   " + value.Age + "   " + value.Salary);
                }
            }
            else
            {
                Console.WriteLine("Web API call failed");
                Console.WriteLine("Reason: " + response.ReasonPhrase);
            }
            #endregion Web API call


            var newEmployee = new Employee
            {
                Name = "John Doe",
                Age = 30,

                Salary = 60000
            };

            await AddEmployeeAsync(client, newEmployee);

            static async Task AddEmployeeAsync(HttpClient client, Employee employee)
            {
                var newEmployee = new
                {
                    pub01_name = employee.Name,
                    pub01_age = employee.Age,
                    pub01_empid = employee.EmpId,
                    pub01_salary = employee.Salary
                };

                var jsonPayload = JsonConvert.SerializeObject(newEmployee);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("pub01_employees", content);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Employee added successfully.");
                }
                else
                {
                    Console.WriteLine("Failed to add employee.");
                    Console.WriteLine("Reason: " + response.ReasonPhrase);
                }
            }


        }
    }

    public class Employee
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public string EmpId { get; set; }
        public int Salary { get; set; }
    }

    public class Value
    {
        public string pub01_name { get; set; }
        public int pub01_age { get; set; }
        public string pub01_empid { get; set; }
        public int pub01_salary { get; set; }
    }

    public class JsonResponse
    {
        public List<Value> Value { get; set; }
    }
}
