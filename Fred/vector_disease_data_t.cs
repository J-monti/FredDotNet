using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class vector_disease_data_t
  {
    public const int VECTOR_DISEASE_TYPES = 4;

    public double vectors_per_host;
    public int N_vectors;
    public int S_vectors;
    public int[] E_vectors = new int[VECTOR_DISEASE_TYPES];
    public int[] I_vectors = new int[VECTOR_DISEASE_TYPES];
    public int[] place_seeds = new int[VECTOR_DISEASE_TYPES];
    public int[] day_start_seed = new int[VECTOR_DISEASE_TYPES];
    public int[] day_end_seed = new int[VECTOR_DISEASE_TYPES];
  }
}
