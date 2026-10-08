using System.Reflection;
using JetBrains.Annotations;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;

namespace WTT_Halloween;

[Injectable(TypePriority = OnLoadOrder.Preload + 3), UsedImplicitly]
public class WTT_Halloween(WTTServerCommonLib.WTTServerCommonLib wttServerCommonLib) : IOnLoad
{
    public async Task OnLoadAsync(CancellationToken cancellationToken)
    {
        var assembly = Assembly.GetExecutingAssembly();
        await wttServerCommonLib.CustomItemServiceExtended.CreateCustomItems(assembly);
    }
}