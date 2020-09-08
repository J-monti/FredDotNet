using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Fred.Tests
{
  [TestClass]
  public class TestFred
  {
    [TestMethod]
    public void RunThatIsh()
    {
      var fred = new FredMain();
      fred.run();
    }
  }
}
