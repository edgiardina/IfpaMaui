using PinballApi.Models.WPPR.v1.Players;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ifpa.Models
{
    public class PlayerSearchResult : Search
    {
        public string StateProvince { get => this.State; }

    }
}
