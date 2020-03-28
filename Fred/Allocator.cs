using System;
using System.Collections.Generic;
using System.Text;

namespace Fred
{
  public class Allocator
  {
    PlaceType[] allocation_array;
    int current_allocation_size;
    int current_allocation_index;
    int number_of_contiguous_blocks_allocated;
    int remaining_allocations;
    int allocations_made;

    Allocator()
    {
      remaining_allocations = 0;
      number_of_contiguous_blocks_allocated = 0;
      allocations_made = 0;
      current_allocation_index = 0;
      current_allocation_size = 0;
      allocation_array = NULL;
    }

    bool reserve(int n = 1)
    {
      if (remaining_allocations == 0)
      {
        current_allocation_size = n;
        allocation_array = new PlaceType[n];
        remaining_allocations = n;
        current_allocation_index = 0;
        ++(number_of_contiguous_blocks_allocated);
        allocations_made += n;
        return true;
      }
      return false;
    }

    PlaceType get_free()
    {
      if (remaining_allocations == 0)
      {
        reserve();
      }
      PlaceType place_pointer = allocation_array + current_allocation_index;
      --(remaining_allocations);
      ++(current_allocation_index);
      return place_pointer;
    }

    int get_number_of_remaining_allocations()
    {
      return remaining_allocations;
    }

    int get_number_of_contiguous_blocks_allocated()
    {
      return number_of_contiguous_blocks_allocated;
    }

    int get_number_of_allocations_made()
    {
      return allocations_made;
    }

    PlaceType get_base_pointer()
    {
      return allocation_array;
    }

    int size()
    {
      return allocations_made;
    }
  }
}
