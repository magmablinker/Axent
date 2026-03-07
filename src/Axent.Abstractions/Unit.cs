namespace Axent.Abstractions;

/// <summary>
/// Use this type when your IRequestHandler should return void
/// </summary>
public sealed class Unit
{
    public static Unit Value => new();
    
    private Unit()
    {
        
    }
}