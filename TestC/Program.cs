using ASiNet.NPlus.Tcp;


var np = new NPlusClient("127.0.0.1", 44544);

var reader = np.GetReader();
var writer = np.GetWriter();

var packId = writer.WritePackage(new byte[] { 20, 20, 20 });

var response = reader.ReadPackage(packId);