using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WcfTestApplication.devcrmmutuaide;

namespace WcfTestApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            // IMutuaideService wcfTest = new MutuaideServiceClient("Soap");
            var exchangeId = new[] { "" };
            //var result  = wcfTest.GetEmailByExchangeId("<8f6a6c$2bftc@mailgate.services.mutu66.lan>");
            MutuaideService t = new MutuaideService();
            
            //var result = t.UpdateCustomerBin("136542499", "513152");
            //Console.Write(result.InnerXml);
            if (string.IsNullOrEmpty(ConfigurationSettings.AppSettings["messageId"])){
                exchangeId = new[]
            {
                "<2V6ZC.FCHS90AD43.20141013113903660@qgate-mail.co.uk>                             ",
                "<7905075c9bd54bd780b914148cb5ef16@DB4PR05MB320.eurprd05.prod.outlook.com>         ",
                "<70b782fbef0e4b36bb280ac9ad73860e@AMXPR05MB0661.eurprd05.prod.outlook.com>        ",
                "<07bfdc2a59d74bb991d5bc422031c806@AMXPR05MB0661.eurprd05.prod.outlook.com>        ",
                "<bbf7d6ffcd2c4df7b00f471a8d4f1812@AMXPR05MB0661.eurprd05.prod.outlook.com>        ",
                "<8f6a6c$2bftc@mailgate.services.mutu66.lan>                                       ",
                "<f6ccb44695f341f2be1e7373fd2c8d69@VMEMAIL1-BRY.mutu.lan>                          ",
                "<AC2B7D79351845828A61C832D2DD3BDA1CFFF16E536D@LIORA.BENDAHAN.JSI-GROUPE.COM>      ",
                "<80394B8A7F2041D09C6612762F2A96E81CFFF1988326@CRM_MAILBOX.JSI-GROUPE.COM>         ",
                "<2648659AC2D147B0837CD05B0199BC691CFFF3EA58A4@LIORA.BENDAHAN.JSI-GROUPE.COM>      ",
                "<578575F18EA04BFAB43439FE7E309ED81CFFF40C4AEC@LIORA.BENDAHAN.JSI-GROUPE.COM>      ",
                "<DBA89F1EC3594CFEBD30335BB5E281A01CFFF414CCC3@JORDAN.ELBAZ.JSI-GROUPE.COM>        ",
                "<C9314605466F47118DE1CE31BDE85D341CFFF43C03F8@JORDAN.ELBAZ.JSI-GROUPE.COM>        ",
                "<F1BF35E5E3DC41B3BA1DE1015CC7AE9A1CFFF43EDB67@JORDAN.ELBAZ.JSI-GROUPE.COM>        ",
                "<6D7CC912DA894F33BEA6CD46CD26B3A31CFFFEC9C7B5@LIORA.BENDAHAN.GMAIL.COM>           ",
                "<C1832ACBADB645E48857ED5E263C25EB1D0001493502@BOB.LEPONGE.CARTOON-NETWORK.COM>    ",
                "<8D75EBD9180240BA99CC66FB2317FDF01D000149367F@BOB.LEPONGE.CARTOON-NETWORK.COM>    ",
                "<C0647ECF05154ECDA2150997B7370B961D00014937FD@BOB.LEPONGE.CARTOON-NETWORK.COM>    ",
                "<19E8D7F320C9435A905DCE263327A8671D0001495530@CONTRACT2285.JSI-GROUPE.COM>        ",
                "<027AE382F82A4BF682A2A45F31AFA0731D0038587DBD@EXGCARMEN.SOCIETE.COM>              ",
                "<2E3935BA6623BD4FABBD26A68E6832A202A0CF8147@IE2RD2XVS111.red002.local>            ",
                "<2E3935BA6623BD4FABBD26A68E6832A202A0CF8177@IE2RD2XVS111.red002.local>            ",
                "<2E3935BA6623BD4FABBD26A68E6832A202A1016D13@IE2RD2XVS111.red002.local>            ",
                "<2E3935BA6623BD4FABBD26A68E6832A202A1016DA4@IE2RD2XVS111.red002.local>            ",
                "<2E3935BA6623BD4FABBD26A68E6832A202A10174FE@IE2RD2XVS111.red002.local>            ",
                "<EFB25A86D4DC9D4A87D329BC6E39732805CCECEBD2@IE2RD2XVS111.red002.local>            ",
                "<EFB25A86D4DC9D4A87D329BC6E39732805CCECEBD3@IE2RD2XVS111.red002.local>            ",
                "<EFB25A86D4DC9D4A87D329BC6E39732805CCECECDD@IE2RD2XVS111.red002.local>            ",
                "<EFB25A86D4DC9D4A87D329BC6E39732805CCECF3E1@IE2RD2XVS111.red002.local>            ",
                "<EFB25A86D4DC9D4A87D329BC6E39732805CCECF623@IE2RD2XVS111.red002.local>            ",
                "<AFFAA4F95B1E904CBA5BA25E5C85574E0805C51CF9@IE2RD2XVS121.red002.local>            ",
                "<AFFAA4F95B1E904CBA5BA25E5C85574E0805CC6AFD@IE2RD2XVS121.red002.local>            ",
                "<7648a77d-ca89-4a9d-b58a-160663320c7d@xtinmta424.xt.local>                        ",
                "<BEB8203D5D47C64494BC71E9E959CCEE09345DAA@IE2RD2XVS231.red002.local>              ",
                "<f3966952-ff9a-4785-b05c-662bf7a53217@xtinmta177.xt.local>                        ",
                "<692337d2-a865-4e1a-ae52-ffde1416968f@xtinmta476.xt.local>                        ",
                "<EFB25A86D4DC9D4A87D329BC6E39732805CEA36F36@IE2RD2XVS111.red002.local>            ",
                "<EFB25A86D4DC9D4A87D329BC6E39732805CEBA2A2C@IE2RD2XVS111.red002.local>            ",
                "<EFB25A86D4DC9D4A87D329BC6E39732805CEBA3308@IE2RD2XVS111.red002.local>            ",
                "<EFB25A86D4DC9D4A87D329BC6E39732805CEBA3345@IE2RD2XVS111.red002.local>            ",
                "<AFFAA4F95B1E904CBA5BA25E5C85574E0805D913C2@IE2RD2XVS121.red002.local>            ",
                "<BEB8203D5D47C64494BC71E9E959CCEE093465D2@IE2RD2XVS231.red002.local>              ",
                "<BEB8203D5D47C64494BC71E9E959CCEE09420395@IE2RD2XVS231.red002.local>              ",
                "<29fafc8a-7813-4bca-a410-a71d24a10c9c@xtinmta476.xt.local>                        ",
                "<AFFAA4F95B1E904CBA5BA25E5C85574E0805D91CB7@IE2RD2XVS121.red002.local>            ",
                "<EFB25A86D4DC9D4A87D329BC6E39732805CEDBBF21@IE2RD2XVS111.red002.local>            ",
                "<6BB13CA042543A44A83EE12369609D182B41124F@IE2RD2XVS221.red002.local>              ",
                "<AFFAA4F95B1E904CBA5BA25E5C85574E0805DF105B@IE2RD2XVS121.red002.local>            ",
                "<6BB13CA042543A44A83EE12369609D182B41162C@IE2RD2XVS221.red002.local>              ",
                "<1132583EE84A474B8EA39EEC710A01672F97F7C5@IE2RD2XVS241.red002.local>              ",
                "<62719DA71265FC4BBC5CB321702704DC01EBDA0166@IE2RD2XVS091.red002.local>            ",
                "<6BB13CA042543A44A83EE12369609D182B636A5F@IE2RD2XVS221.red002.local>              ",
                "<878AF3F973C7B14F9010F33D786109C503FED6F636@IE2RD2XVS101.red002.local>            ",
                "<EFB25A86D4DC9D4A87D329BC6E39732805CEFC2D1E@IE2RD2XVS111.red002.local>            ",
                "<BEB8203D5D47C64494BC71E9E959CCEE095EB068@IE2RD2XVS231.red002.local>              ",
                "<878AF3F973C7B14F9010F33D786109C503FED6F7BB@IE2RD2XVS101.red002.local>            ",
                "<878AF3F973C7B14F9010F33D786109C503FEDEBE11@IE2RD2XVS101.red002.local>            ",
                "<878AF3F973C7B14F9010F33D786109C503FEDEBEC7@IE2RD2XVS101.red002.local>            ",
                "<878AF3F973C7B14F9010F33D786109C503FEDEBECF@IE2RD2XVS101.red002.local>            ",
                "<878AF3F973C7B14F9010F33D786109C503FEDEC0C6@IE2RD2XVS101.red002.local>            "
            };
            }
            else
            {
                exchangeId = ConfigurationSettings.AppSettings["messageId"].Split(',');
            }

            foreach (string s in exchangeId)
            {
                callAsync(t, s);
            }
            //var x = t.GetEmailByExchangeId("<70b782fbef0e4b36bb280ac9ad73860e@AMXPR05MB0661.eurprd05.prod.outlook.com>");
            //Console.Write(x.InnerXml);
            Console.Read();
        }

        public static void callAsync(MutuaideService service, string messageId)
        {
            Console.WriteLine("send...");
            service.GetEmailByExchangeIdAsync("<" + messageId + ">");

        }

        public async static Task<string> GetEmailByExchangeId(MutuaideService service, string messageId)
        {
            return service.GetEmailByExchangeId(messageId).InnerXml;
        }
    }
}
