using Kasa;

namespace Test;

[Collection(nameof(KasaOutletFactoryTest))]
[CollectionDefinition(nameof(KasaOutletFactoryTest), DisableParallelization = true)]
public class KasaOutletFactoryTest: IDisposable {

    private readonly Func<string, Options?, IKasaOutlet>            _originalSingleSocketOutletFactory = KasaOutletFactory.SingleSocketOutletFactory;
    private readonly Func<string, Options?, IMultiSocketKasaOutlet> _originalMultiSocketOutletFactory  = KasaOutletFactory.MultiSocketOutletFactory;

    private readonly IKasaOutlet            _mockSingleSocketOutlet = A.Fake<IKasaOutlet>();
    private readonly IMultiSocketKasaOutlet _mockMultiSocketOutlet  = A.Fake<IMultiSocketKasaOutlet>();

    public KasaOutletFactoryTest() {
        KasaOutletFactory.SingleSocketOutletFactory = (_, _) => _mockSingleSocketOutlet;
        KasaOutletFactory.MultiSocketOutletFactory  = (_, _) => _mockMultiSocketOutlet;
    }

    [Fact]
    public async Task CreateSingleSocketOutlet() {
        A.CallTo(() => _mockSingleSocketOutlet.System.CountSockets()).Returns(1);

        IKasaOutletBase actual = await KasaOutletFactory.CreateOutlet("192.168.1.100");

        actual.Should().BeAssignableTo<IKasaOutlet>().And.NotBeAssignableTo<IMultiSocketKasaOutlet>();
        A.CallTo(() => _mockSingleSocketOutlet.Dispose()).MustNotHaveHappened();
    }

    [Fact]
    public async Task CreateMultiSocketOutlet() {
        A.CallTo(() => _mockSingleSocketOutlet.System.CountSockets()).Returns(2);

        IKasaOutletBase actual = await KasaOutletFactory.CreateOutlet("192.168.1.100");

        actual.Should().BeAssignableTo<IMultiSocketKasaOutlet>();
        A.CallTo(() => _mockSingleSocketOutlet.Dispose()).MustHaveHappened();
    }

    public void Dispose() {
        KasaOutletFactory.SingleSocketOutletFactory = _originalSingleSocketOutletFactory;
        KasaOutletFactory.MultiSocketOutletFactory  = _originalMultiSocketOutletFactory;
        GC.SuppressFinalize(this);
    }

}