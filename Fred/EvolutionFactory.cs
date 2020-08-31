using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public static class EvolutionFactory
  {
    public static Evolution newEvolution(int type)
    {
      switch (type)
      {
        case 0:
          return new Evolution();
        case 1:
          return new MSEvolution();
        default:
          Utils.FRED_STATUS(0, "Unknown Evolution type ({0}) supplied to EvolutionFactory.  Using the default.", type);
          return new Evolution();
      }
    }
  }
}
