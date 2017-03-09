using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WcfTest.MutuaideService;

namespace WcfTest
{
    class Program
    {
        static void Main(string[] args)
        {
            //MutuaideService wcfClient = new MutuaideService();
            IMutuaideService wcfService = new MutuaideServiceClient();
           
            //wcfService.
            
        }
    }
}
