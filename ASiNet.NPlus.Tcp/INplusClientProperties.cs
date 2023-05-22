using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASiNet.NPlus.Tcp;
public interface INplusClientProperties
{
    /// <summary>
    /// <see cref="true"/> если подключён к удалённому сокету, иначе <see cref="false"/>.
    /// </summary>
    public bool IsConnected { get; }
    /// <summary>
    /// <see cref="true"/> если есть в потоке данные для чтения, иначе <see cref="false"/>.
    /// </summary>
    public bool DataAvalible { get; }
    /// <summary>
    /// Размер данных доступных для чтения из потока.
    /// </summary>
    public int AvalibleBytes { get; }
    /// <summary>
    /// Количество успешно принятых пакетов.
    /// </summary>
    public int AcceptedPackages { get; }
    /// <summary>
    /// Количество успешно отправленных пакетов.
    /// </summary>
    public int SendedPackages { get; }
    /// <summary>
    /// Количество успешно принятых байт.
    /// </summary>
    public long AcceptedBytes { get; }
    /// <summary>
    /// Количество успешно отправленых байт.
    /// </summary>
    public long SendedBytes { get; }

    public int PackagesInBuffer { get; }

    public TimeSpan AcceptTimeout { get; set; }
}
