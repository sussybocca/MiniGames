namespace MiniGames.Dynamic;

public interface ICanvas
{
    void Clear();
    void FillRect(int x, int y, int w, int h, string color);
    void DrawText(string text, int x, int y);
}

public abstract class AppBase
{
    public virtual void Draw(ICanvas canvas) { }
    public virtual void OnMouseDown(int x, int y) { }
    public virtual void OnMouseMove(int x, int y) { }
    public virtual void OnMouseUp(int x, int y) { }
}