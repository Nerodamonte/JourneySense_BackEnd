using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
namespace JSEA_Presentation.Swagger;

/// <summary>
/// Thêm ví dụ request/response mẫu vào Swagger UI cho các API.
/// </summary>
public class SwaggerExamplesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var method = context.ApiDescription.ActionDescriptor.RouteValues["action"];
        var bodyParam = context.ApiDescription.ParameterDescriptions
            .FirstOrDefault(p => p.Source.Id == "Body");
        var requestType = bodyParam?.Type;

        // Request body example
        if (operation.RequestBody?.Content?.ContainsKey("application/json") == true && requestType != null)
        {
            var example = GetRequestExample(requestType);
            if (example != null)
                operation.RequestBody.Content["application/json"].Example = example;
        }

        // Response examples (200, 201)
        var path = context.ApiDescription.RelativePath ?? "";
        var actionName = method ?? "";
        if (operation.Responses.TryGetValue("200", out var res200))
            SetResponseExample(res200, path, actionName, 200);
        if (operation.Responses.TryGetValue("201", out var res201))
            SetResponseExample(res201, path, actionName, 201);
    }

    private static IOpenApiAny? GetRequestExample(Type requestType)
    {
        var name = requestType.Name;
        return name switch
        {
            "LoginRequest" => new OpenApiObject
            {
                ["email"] = new OpenApiString("traveler@example.com"),
                ["password"] = new OpenApiString("YourPassword123!")
            },
            "RegisterEmailRequest" => new OpenApiObject
            {
                ["email"] = new OpenApiString("newuser@example.com")
            },
            "VerifyOtpRequest" => new OpenApiObject
            {
                ["otp"] = new OpenApiString("123456")
            },
            "SetPasswordRequest" => new OpenApiObject
            {
                ["password"] = new OpenApiString("YourNewPassword123!"),
                ["confirmPassword"] = new OpenApiString("YourNewPassword123!")
            },
            "CreatePaymentRequest" => new OpenApiObject
            {
                ["totalAmount"] = new OpenApiLong(100000),
                ["description"] = new OpenApiString("Thanh toán gói trải nghiệm JourneySense"),
                ["returnUrl"] = new OpenApiString("https://yourapp.com/payment/success"),
                ["cancelUrl"] = new OpenApiString("https://yourapp.com/payment/cancel"),
                ["buyerName"] = new OpenApiString("Nguyễn Văn A"),
                ["buyerEmail"] = new OpenApiString("buyer@example.com"),
                ["buyerPhone"] = new OpenApiString("0901234567"),
                ["items"] = new OpenApiArray
                {
                    new OpenApiObject
                    {
                        ["name"] = new OpenApiString("Gói trải nghiệm 1 ngày"),
                        ["quantity"] = new OpenApiInteger(1),
                        ["price"] = new OpenApiLong(100000),
                        ["unit"] = new OpenApiString("gói")
                    }
                }
            },
            "JourneySetupRequest" => new OpenApiObject
            {
                ["originAddress"] = new OpenApiString("1 Công xã Paris, Bến Nghé, Quận 1, TP.HCM"),
                ["destinationAddress"] = new OpenApiString("Bưu điện Trung tâm Sài Gòn, Quận 1, TP.HCM"),
                ["vehicleType"] = new OpenApiInteger(3), // Car
                ["timeBudgetMinutes"] = new OpenApiInteger(60),
                ["maxDetourDistanceMeters"] = new OpenApiInteger(2000)
            },
            "VisitFeedbackRequest" => new OpenApiObject
            {
                ["experienceId"] = new OpenApiString("00000000-0000-0000-0000-000000000001"),
                ["journeyId"] = new OpenApiString("00000000-0000-0000-0000-000000000002"),
                ["ratingValue"] = new OpenApiInteger(5),
                ["feedbackText"] = new OpenApiString("Trải nghiệm tuyệt vời, view đẹp và nhân viên thân thiện."),
                ["photoUrls"] = new OpenApiArray
                {
                    new OpenApiString("https://example.com/photos/photo1.jpg"),
                    new OpenApiString("https://example.com/photos/photo2.jpg")
                }
            },
            "CreateMicroExperienceRequest" => new OpenApiObject
            {
                ["name"] = new OpenApiString("Cà phê View Sài Gòn"),
                ["categoryId"] = new OpenApiString("00000000-0000-0000-0000-000000000001"),
                ["address"] = new OpenApiString("123 Nguyễn Huệ, Quận 1, TP.HCM"),
                ["city"] = new OpenApiString("Hồ Chí Minh"),
                ["country"] = new OpenApiString("Việt Nam")
            },
            "UpdateMicroExperienceRequest" => new OpenApiObject
            {
                ["name"] = new OpenApiString("Cà phê View Sài Gòn (Đã cập nhật)"),
                ["categoryId"] = new OpenApiString("00000000-0000-0000-0000-000000000001"),
                ["address"] = new OpenApiString("123 Nguyễn Huệ, Quận 1, TP.HCM"),
                ["status"] = new OpenApiInteger(1) // Verified
            },
            _ => null
        };
    }

    private static void SetResponseExample(OpenApiResponse response, string path, string method, int statusCode)
    {
        if (response.Content?.ContainsKey("application/json") != true) return;

        IOpenApiAny? example = null;
        if (path.Contains("auth/login"))
            example = new OpenApiObject
            {
                ["userId"] = new OpenApiString("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
                ["email"] = new OpenApiString("traveler@example.com"),
                ["role"] = new OpenApiString("Traveler"),
                ["accessToken"] = new OpenApiString("eyJhbGciOiJIUzI1NiIs..."),
                ["refreshToken"] = new OpenApiString("eyJhbGciOiJIUzI1NiIs...")
            };
        else if (path.Contains("register/verify-otp"))
            example = new OpenApiObject { ["registerToken"] = new OpenApiString("eyJhbGciOiJIUzI1NiIs...") };
        else if (path.Contains("register/send-otp") || path.Contains("register/resend-otp"))
            example = new OpenApiObject { ["message"] = new OpenApiString("Mã OTP đã được gửi!") };
        else if (path.Contains("register/set-password"))
            example = new OpenApiObject { ["message"] = new OpenApiString("Thiết lập mật khẩu thành công. Vui lòng đăng nhập.") };
        else if (path.Contains("Payment/create"))
            example = new OpenApiObject
            {
                ["orderCode"] = new OpenApiLong(123456789),
                ["paymentLinkId"] = new OpenApiString("abc123xyz"),
                ["checkoutUrl"] = new OpenApiString("https://pay.payos.vn/..."),
                ["qrCode"] = new OpenApiString("data:image/png;base64,..."),
                ["status"] = new OpenApiString("PENDING"),
                ["amount"] = new OpenApiLong(100000),
                ["description"] = new OpenApiString("Thanh toán gói trải nghiệm")
            };
        else if (path.Contains("Payment/link/") && method != "CancelPaymentLink")
            example = new OpenApiObject
            {
                ["paymentLinkId"] = new OpenApiString("abc123xyz"),
                ["status"] = new OpenApiString("PENDING"),
                ["amount"] = new OpenApiLong(100000),
                ["amountPaid"] = new OpenApiLong(0),
                ["amountRemaining"] = new OpenApiLong(100000),
                ["description"] = new OpenApiString("Thanh toán gói trải nghiệm")
            };
        else if (path.Contains("journeys/setup"))
            example = new OpenApiObject
            {
                ["journeyId"] = new OpenApiString("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
                ["status"] = new OpenApiString("Planning"),
                ["summary"] = new OpenApiString("Quận 1, TP.HCM → Bưu điện Sài Gòn, ~15 phút bằng ô tô.")
            };
        else if (path.Contains("experiences/visit"))
            example = new OpenApiObject
            {
                ["visitId"] = new OpenApiString("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
                ["ratingId"] = new OpenApiString("3fa85f64-5717-4562-b3fc-2c963f66afa7"),
                ["feedbackId"] = new OpenApiString("3fa85f64-5717-4562-b3fc-2c963f66afa8"),
                ["pointsEarned"] = new OpenApiInteger(10)
            };
        else if (path.Contains("micro-experiences"))
        {
            if (!path.Contains("{id}"))
                example = new OpenApiArray
                {
                    new OpenApiObject
                    {
                        ["id"] = new OpenApiString("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
                        ["name"] = new OpenApiString("Cà phê View Sài Gòn"),
                        ["city"] = new OpenApiString("Hồ Chí Minh"),
                        ["status"] = new OpenApiString("Verified")
                    }
                };
            else
                example = new OpenApiObject
                {
                    ["id"] = new OpenApiString("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
                    ["name"] = new OpenApiString("Cà phê View Sài Gòn"),
                    ["categoryName"] = new OpenApiString("Ăn uống"),
                    ["description"] = new OpenApiString("Quán view đẹp, phù hợp chụp ảnh."),
                    ["avgRating"] = new OpenApiDouble(4.5),
                    ["status"] = new OpenApiString("Verified"),
                    ["address"] = new OpenApiString("123 Nguyễn Huệ, Quận 1, TP.HCM"),
                    ["city"] = new OpenApiString("Hồ Chí Minh"),
                    ["country"] = new OpenApiString("Việt Nam")
                };
        }

        if (example != null)
            response.Content["application/json"].Example = example;
    }
}
