using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASiNet.NPlus.Tcp;
public interface ITypedNPlusClient : INPlusClientProperties
{
    public uint SerializerBufferSize { get; set; }

    public ResponsePackage<TOut> SendAndWaitResponse<TIn, TOut>(TIn data, CancellationToken token = default) where TOut : new();
    public Task<ResponsePackage<TOut>> SendAndWaitResponseAsync<TIn, TOut>(TIn data, CancellationToken token = default) where TOut : new();

    public NPlusStatus SendResponse<TOut>(Guid id, TOut data);

    public RequestPackage<TOut> AcceptNext<TOut>() where TOut : new();

    public Task<RequestPackage<TOut>> AcceptNextAsync<TOut>(CancellationToken token = default) where TOut : new();
}
