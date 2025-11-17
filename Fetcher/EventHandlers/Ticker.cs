namespace Fetcher.EventHandlers;
public class Ticker
{
    public readonly int milliseconds;
    private readonly Task? Clock;
    private readonly CancellationToken cancellationToken;
    public event EventHandler? Ticked;

    protected virtual void OnTick(EventArgs? e = null)
    {
        Ticked?.Invoke(this, e ?? EventArgs.Empty);
    }

    public Ticker(int milliseconds = 5000, CancellationToken? token = null)
    {
        cancellationToken = token ?? CancellationToken.None;
        this.milliseconds = milliseconds;
        Clock = Task.Run( async ()=>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                OnTick();
                await Task.Delay(this.milliseconds, cancellationToken);
            }
        });
    }
}