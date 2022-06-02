using System.Collections.Immutable;
using System.Net.NetworkInformation;
using FluentAssertions;
using Kasa;
using Kasa.Marshal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Test;

public class MarshalTests {

    [Fact]
    public void ParseFeature() {
        JsonConvert.DeserializeObject<ISet<Feature>>(@"""TIM""", new FeatureConverter()).Should().BeEquivalentTo(ImmutableHashSet.Create(Feature.Timer));
    }

    [Fact]
    public void ParseFeatures() {
        JsonConvert.DeserializeObject<ISet<Feature>>(@"""TIM:ENE""", new FeatureConverter()).Should().BeEquivalentTo(ImmutableHashSet.Create(Feature.EnergyMeter, Feature.Timer));
    }

    [Theory]
    [InlineData(@"""none""", OperatingMode.None)]
    [InlineData(@"""schedule""", OperatingMode.Schedule)]
    [InlineData(@"""count_down""", OperatingMode.Timer)]
    public void ParseOperatingMode(string input, OperatingMode expected) {
        JsonConvert.DeserializeObject<OperatingMode>(input, new OperatingModeConverter()).Should().Be(expected);
    }

    [Fact]
    public void ParsePhysicalAddress() {
        JsonConvert.DeserializeObject<PhysicalAddress>(@"""5C:A6:E6:4E:F3:EF""", new MacAddressConverter()).Should().Be(new PhysicalAddress(new byte[] { 0x5C, 0xA6, 0xE6, 0x4E, 0xF3, 0xEF }));
    }

    [Fact]
    public void WrongJsonTokenType() {
        ((Func<ISet<Feature>?>) (() => JsonConvert.DeserializeObject<ISet<Feature>>(@"1", new FeatureConverter()))).Should().Throw<JsonSerializationException>();
        ((Func<OperatingMode?>) (() => JsonConvert.DeserializeObject<OperatingMode>(@"1", new OperatingModeConverter()))).Should().Throw<JsonSerializationException>();
        ((Func<PhysicalAddress?>) (() => JsonConvert.DeserializeObject<PhysicalAddress>(@"1", new MacAddressConverter()))).Should().Throw<JsonSerializationException>();
        ((Func<PhysicalAddress?>) (() => JsonConvert.DeserializeObject<PhysicalAddress>(@"""abc""", new MacAddressConverter()))).Should().Throw<JsonSerializationException>();
    }

    [Fact]
    public void InvalidEnum() {
        ((Action) (() => Features.FromJsonString(""))).Should().Throw<ArgumentOutOfRangeException>();
        ((Action) (() => OperatingModes.FromJsonString(""))).Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void SystemInfo() {
        const string json =
            @"{""sw_ver"":""1.0.2 Build 200915 Rel.085940"",""hw_ver"":""1.0"",""model"":""EP10(US)"",""deviceId"":""8006C153CFEBDE93CD3572549B5A47611F49F0D2"",""oemId"":""41372DE62C896B2C0E93C20D70B62DDB"",""hwId"":""AE6865C67F6A54B756C0B5812472C825"",""rssi"":-61,""latitude_i"":0,""longitude_i"":0,""alias"":""SX20"",""status"":""configured"",""obd_src"":""tplink"",""mic_type"":""IOT.SMARTPLUGSWITCH"",""feature"":""TIM"",""mac"":""5C:A6:E6:4E:F3:EF"",""updating"":0,""led_off"":0,""relay_state"":0,""on_time"":0,""icon_hash"":"""",""dev_name"":""Smart Wi-Fi Plug Mini"",""active_mode"":""schedule"",""next_action"":{""type"":-1},""err_code"":0}";

        SystemInfo actual = KasaClient.JsonSerializer.Deserialize<SystemInfo>(new JsonTextReader(new StringReader(json)));
        actual.DeviceId.Should().Be("8006C153CFEBDE93CD3572549B5A47611F49F0D2");
        actual.Features.Should().BeEquivalentTo(ImmutableHashSet.Create(Feature.Timer));
        actual.HardwareId.Should().Be("AE6865C67F6A54B756C0B5812472C825");
        actual.HardwareVersion.Should().Be("1.0");
        actual.IndicatorLightDisabled.Should().BeFalse();
        actual.IsOutletOn.Should().BeFalse();
        actual.MacAddress.Should().Be(new PhysicalAddress(new byte[] { 0x5C, 0xA6, 0xE6, 0x4E, 0xF3, 0xEF }));
        actual.ModelName.Should().Be("EP10(US)");
        actual.ModelFamily.Should().Be("Smart Wi-Fi Plug Mini");
        actual.Name.Should().Be("SX20");
        actual.OemId.Should().Be("41372DE62C896B2C0E93C20D70B62DDB");
        actual.Rssi.Should().Be(-61);
        actual.SoftwareVersion.Should().Be("1.0.2 Build 200915 Rel.085940");
        actual.Updating.Should().BeFalse();
    }

    [Theory]
    [InlineData(CommandFamily.AwayMode, "anti_theft")]
    [InlineData(CommandFamily.Cloud, "cnCloud")]
    [InlineData(CommandFamily.Timer, "count_down")]
    [InlineData(CommandFamily.EnergyMeter, "emeter")]
    [InlineData(CommandFamily.NetworkInterface, "netif")]
    [InlineData(CommandFamily.Scheduling, "schedule")]
    [InlineData(CommandFamily.System, "system")]
    [InlineData(CommandFamily.Time, "time")]
    internal void CommandFamilies(CommandFamily family, string expected) {
        family.ToJsonString().Should().Be(expected);
        Enum.GetNames<CommandFamily>().Length.Should().Be(8);
    }

    [Fact]
    public void ReadOnlyConverters() {
        JsonSerializer jsonSerializer = JsonSerializer.CreateDefault();
        new FeatureConverter().WriteJson(new JTokenWriter(), ImmutableHashSet<Feature>.Empty, jsonSerializer);
        new MacAddressConverter().WriteJson(new JTokenWriter(), PhysicalAddress.None, jsonSerializer);
        new OperatingModeConverter().WriteJson(new JTokenWriter(), OperatingMode.Schedule, jsonSerializer);
    }

}