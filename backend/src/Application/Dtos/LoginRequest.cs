namespace Application.Dtos;

// DTO = Data Transfer Object. Es literalmente tu z.object({ email: z.string(), password: z.string() })
public class LoginRequest
{
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
}