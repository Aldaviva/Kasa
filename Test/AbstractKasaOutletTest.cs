using FakeItEasy;
using Kasa;

namespace Test;

public abstract class AbstractKasaOutletTest {

    internal readonly  IKasaClient Client = A.Fake<IKasaClient>();
    protected readonly KasaOutlet  Outlet;

    protected AbstractKasaOutletTest() {
        Outlet = new KasaOutlet(Client);
    }

}