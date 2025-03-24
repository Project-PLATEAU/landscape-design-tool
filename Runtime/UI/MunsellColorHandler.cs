using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;
using UnityEngine.UI;
using LandscapeDesignTool;
using LandScapeDesignTool;
using UnityEngine.EventSystems;

public class MunsellColorHandler : MonoBehaviour
{

    [SerializeField] private GameObject mapButtons;
    [SerializeField] Slider hueslider;
    [SerializeField] GameObject bbox;
    [SerializeField] private RGBColorSelectUI rgbColorSelectUI;
    [SerializeField] private RawImage colorSample;

    bool isEdit = false;
    GameObject targetObject;

    string materialName = "DesignToolMaterial";
    GameObject _bbox = null;

    private string[] huestring =
    {
        "2.5R", "5R", "7.5R", "10R",
        "2.5YR", "5YR", "7.5YR", "10YR",
        "2.5Y", "5Y", "7.5Y", "10Y",
        "2.5GY", "5GY", "7.5GY", "10GY",
        "2.5G", "5G", "7.5G", "10G",
        "2.5BG", "5BG", "7.5BG", "10BG",
        "2.5B", "5B", "7.5B", "10B",
        "2.5PB", "5PB", "7.5PB", "10PB",
        "2.5P", "5P", "7.5P", "10P",
        "2.5RP", "5RP", "7.5RP", "10RP"
    };

    private int hval;

    private List<List<string>> codemap = new List<List<string>>();
    private List<List<string>> codemap25R = new List<List<string>>();
    private List<List<string>> codemap5R = new List<List<string>>();
    private List<List<string>> codemap75R = new List<List<string>>();
    private List<List<string>> codemap10R = new List<List<string>>();
    private List<List<string>> codemap25YR = new List<List<string>>();
    private List<List<string>> codemap5YR = new List<List<string>>();
    private List<List<string>> codemap75YR = new List<List<string>>();
    private List<List<string>> codemap10YR = new List<List<string>>();
    private List<List<string>> codemap25Y = new List<List<string>>();
    private List<List<string>> codemap5Y = new List<List<string>>();
    private List<List<string>> codemap75Y = new List<List<string>>();
    private List<List<string>> codemap10Y = new List<List<string>>();
    private List<List<string>> codemap25GY = new List<List<string>>();
    private List<List<string>> codemap5GY = new List<List<string>>();
    private List<List<string>> codemap75GY = new List<List<string>>();
    private List<List<string>> codemap10GY = new List<List<string>>();
    private List<List<string>> codemap25G = new List<List<string>>();
    private List<List<string>> codemap5G = new List<List<string>>();
    private List<List<string>> codemap75G = new List<List<string>>();
    private List<List<string>> codemap10G = new List<List<string>>();
    private List<List<string>> codemap25BG = new List<List<string>>();
    private List<List<string>> codemap5BG = new List<List<string>>();
    private List<List<string>> codemap75BG = new List<List<string>>();
    private List<List<string>> codemap10BG = new List<List<string>>();
    private List<List<string>> codemap25B = new List<List<string>>();
    private List<List<string>> codemap5B = new List<List<string>>();
    private List<List<string>> codemap75B = new List<List<string>>();
    private List<List<string>> codemap10B = new List<List<string>>();
    private List<List<string>> codemap25PB = new List<List<string>>();
    private List<List<string>> codemap5PB = new List<List<string>>();
    private List<List<string>> codemap75PB = new List<List<string>>();
    private List<List<string>> codemap10PB = new List<List<string>>();
    private List<List<string>> codemap25P = new List<List<string>>();
    private List<List<string>> codemap5P = new List<List<string>>();
    private List<List<string>> codemap75P = new List<List<string>>();
    private List<List<string>> codemap10P = new List<List<string>>();
    private List<List<string>> codemap25RP = new List<List<string>>();
    private List<List<string>> codemap5RP = new List<List<string>>();
    private List<List<string>> codemap75RP = new List<List<string>>();
    private List<List<string>> codemap10RP = new List<List<string>>();

    // private string[,] code25R = [["F2E2E2", "FFDADA","FFD3D2" ],["D7C6C6","ECC0BF","FDB9B9","FFA9AD"]]; 
    // Start is called before the first frame update
    void Start()
    {
        codemap25R.Add(new List<string> { "F2E2E2", "FFDADA", "FFD3D2" });
        codemap25R.Add(new List<string> { "D7C6C6", "ECC0BF", "FDB9B9", "FFA9AD" });
        codemap25R.Add(new List<string> { "BFABAB", "D0A5A5", "E09EA0", "F0979A", "FE8F95", "FF858F", "FF7B8A", "FF6E85" });
        codemap25R.Add(new List<string> { "A59090", "B68A6B", "C68486", "D37D81", "E1757C", "EE6B77", "F96174", "FF526F", "FF406C" });
        codemap25R.Add(new List<string> { "8B7677", "9C7072", "AB696D", "B86168", "C55864", "C24C60", "DC405D", "E82D5a" });
        codemap25R.Add(new List<string> { "735B5D", "825558", "904E54", "9C4551", "A83B4D", "B32D4A", "BE1547" });
        codemap25R.Add(new List<string> { "5B4244", "6A3B40", "75333D", "82283B", "8D1737" });
        codemap25R.Add(new List<string> { "422C3D", "4D252F", "571D3E", "620E2E" });
        codemap25R.Add(new List<string> { "2D161E", "360E1F", "3E0020" });

        codemap5R.Add(new List<string> { "F4E2DF", "FFDAD5", "FFD3CB" });
        codemap5R.Add(new List<string> { "D8C6C4", "EDC0BB", "FEB9B3", "FFB1AA", "FFA9A1" });
        codemap5R.Add(new List<string> { "C0ABA9", "D1A5A1", "E29E99", "F19792", "FF8F8A", "FF8682", "FF7C7A" });
        codemap5R.Add(new List<string> { "A6908F", "B78A87", "C7847F", "D47D79", "E27571", "EF6B6A", "FB6163", "FF545C", "FF4455" });
        codemap5R.Add(new List<string> { "8C7675", "9D706D", "AC6966", "B9615F", "C65857", "D34D51", "DD414B", "E82F44", "F4083D" });
        codemap5R.Add(new List<string> { "745B5D", "835553", "914E4D", "9D4646", "A83C40", "B3303A", "BE1A34" });
        codemap5R.Add(new List<string> { "5C4242", "6A3B3C", "763436", "822930", "8D192B" });
        codemap5R.Add(new List<string> { "422C2E", "4E252B", "581D3B", "630E26" });
        codemap5R.Add(new List<string> { "2D161B", "370E1B" });

        codemap75R.Add(new List<string> { "F5E1DD", "FFDACF", "FFD3C2" });
        codemap75R.Add(new List<string> { "D9C6C2", "EFBFB6", "FFB9AB", "FFB29E", "FFAA92" });
        codemap75R.Add(new List<string> { "C1ABA7", "D3A59C", "E29F92", "F39887", "FF917C", "FF8870", "FF7F64", "FF7457" });
        codemap75R.Add(new List<string> { "A7908C", "B88A82", "C78479", "D57D6F", "E37564", "EF6D59", "FA644D", "FF5841", "FF4B33" });
        codemap75R.Add(new List<string> { "8C7673", "9D7069", "AC695F", "BA6255", "C6594B", "D24F40", "DC4436", "E7352A", "F0201E" });
        codemap75R.Add(new List<string> { "745B59", "845550", "914E47", "9D463D", "A83E35", "B2322B", "BC2221" });
        codemap75R.Add(new List<string> { "5C4340", "6A3C37", "763530", "812B27", "8C1D1E" });
        codemap75R.Add(new List<string> { "422C2C", "4E2527", "581E22", "63101D" });
        codemap75R.Add(new List<string> { "2E1619", "380E17" });

        codemap10R.Add(new List<string> { "F5E1DB", "FFDAC9", "FFD4B9" });
        codemap10R.Add(new List<string> { "DAC6C0", "EFC0B0", "FFBAA1", "FFB392", "FFAD83" });
        codemap10R.Add(new List<string> { "C1ABA4", "D3A597", "E3A089", "F2997B", "FE936D", "FF8C5B", "FF8449", "FF7C31" });
        codemap10R.Add(new List<string> { "A7908A", "B88B7C", "C78570", "D47F63", "E17853", "EC7143", "F7692F", "FF6114" });
        codemap10R.Add(new List<string> { "8D7670", "9D7063", "AC6A56", "B96449", "C45C39", "CF5428", "D74D12" });
        codemap10R.Add(new List<string> { "745C56", "84564A", "90503F", "9C4932", "A64124", "AF3812" });
        codemap10R.Add(new List<string> { "5B433E", "693D34", "753629", "802D1E", "892211" });
        codemap10R.Add(new List<string> { "422C2A", "4E2622", "581F1A", "621312" });
        codemap10R.Add(new List<string> { "2E1617", "380E12" });

        codemap25YR.Add(new List<string> { "F7E1D7", "FFDBC2", "FFD6AF" });
        codemap25YR.Add(new List<string> { "DBC6BD", "EFC1AA", "FEBC99", "FFB687", "FFB174", "FFAB5F" });
        codemap25YR.Add(new List<string> { "C1ABA1", "D2A691", "E1A180", "ET9C70", "FA965F", "FF9148", "FF8B2C" });
        codemap25YR.Add(new List<string> { "A89086", "B78C78", "C58668", "D28158", "DD7C45", "E6762F", "EE710A" });
        codemap25YR.Add(new List<string> { "8D766D", "9C715F", "AA6C4F", "B6663D", "BF612A", "C75C0F" });
        codemap25YR.Add(new List<string> { "745C53", "825745", "8E5237", "984C36", "A0470F" });
        codemap25YR.Add(new List<string> { "5B433B", "673E2F", "723820", "7B320E" });
        codemap25YR.Add(new List<string> { "412D27", "4D271D", "572111" });
        codemap25YR.Add(new List<string> { "2D1714", "380F0A", "410302" });

        codemap5YR.Add(new List<string> { "F7E2D2", "FFDDBB", "FFD9A7" });
        codemap5YR.Add(new List<string> { "DBC6B8", "ECC2A4", "FABE91", "FFBA7B", "FFB666", "FFB24D", "FFAE2B" });
        codemap5YR.Add(new List<string> { "C1AB9E", "D0A78B", "DEA378", "E99F65", "F39B4F", "FD9734" });
        codemap5YR.Add(new List<string> { "A79183", "B58D72", "C2895F", "CD844B", "D68035", "DD7D15" });
        codemap5YR.Add(new List<string> { "8C776A", "9A7359", "A66E46", "B06A32", "B86619" });
        codemap5YR.Add(new List<string> { "735D50", "805840", "8A542F", "92501B" });
        codemap5YR.Add(new List<string> { "594439", "65402A", "6E3B19" });
        codemap5YR.Add(new List<string> { "402D25", "4B2917", "542401" });
        codemap5YR.Add(new List<string> { "2C1810" });

        codemap75YR.Add(new List<string> { "F6E2CE", "FFDFB6", "FFDC9F", "FFD98D" });
        codemap75YR.Add(new List<string> { "DAC7B5", "E9C49F", "F6C08A", "FFBD72", "FFBA5A", "FFB73C" });
        codemap75YR.Add(new List<string> { "C0AC9B", "CEA986", "DAA671", "E3A35B", "ECA042", "F39D1C" });
        codemap75YR.Add(new List<string> { "A69281", "B38E6E", "BE8B58", "C78841", "CF8524" });
        codemap75YR.Add(new List<string> { "8B7768", "987455", "A3713F", "AB6D27" });
        codemap75YR.Add(new List<string> { "725D4E", "7E5A3B", "865728", "8C540E" });
        codemap75YR.Add(new List<string> { "584537", "624226", "693F11" });
        codemap75YR.Add(new List<string> { "3F2E23", "482B12" });
        codemap75YR.Add(new List<string> { "2B190D" });

        codemap10YR.Add(new List<string> { "F4E3CA", "FFE1B0", "FFDF98", "FFDD7E" });
        codemap10YR.Add(new List<string> { "D8C8B1", "E4C69A", "EFC482", "F8C168", "FFBF4D", "FFBD28" });
        codemap10YR.Add(new List<string> { "BEAD97", "CAAB80", "D4A969", "DCA650", "E3A433" });
        codemap10YR.Add(new List<string> { "A3937E", "AE9068", "B88E50", "C08C36", "C68A10" });
        codemap10YR.Add(new List<string> { "897865", "94764F", "9D7337", "A4711B" });
        codemap10YR.Add(new List<string> { "6F5F4C", "7A5C36", "815A21" });
        codemap10YR.Add(new List<string> { "554636", "5E4423", "644108" });
        codemap10YR.Add(new List<string> { "3D2F21", "452D0D" });
        codemap10YR.Add(new List<string> { "291A0A" });

        codemap25Y.Add(new List<string> { "F1E5C7", "FBE4AC", "FFE292", "FFE177", "FFE05A", "FFDE34" });
        codemap25Y.Add(new List<string> { "D6C9AE", "DFC896", "E8C77C", "EFC561", "F6C442", "FBC30A" });
        codemap25Y.Add(new List<string> { "BBAE95", "C5AD7D", "CDAB63", "D4AA48", "D9A923" });
        codemap25Y.Add(new List<string> { "A0947C", "AA9264", "B2914A", "B88F2C" });
        codemap25Y.Add(new List<string> { "867964", "8F784B", "977630", "9C750D" });
        codemap25Y.Add(new List<string> { "6C604A", "755E33", "7B5D1A" });
        codemap25Y.Add(new List<string> { "524734", "5A461F" });
        codemap25Y.Add(new List<string> { "3A3020", "412E07" });
        codemap25Y.Add(new List<string> { "261B07" });

        codemap5Y.Add(new List<string> { "EEE6C5", "F5E6A9", "FBE68D", "FFE670", "FFE550", "FFE51D" });
        codemap5Y.Add(new List<string> { "D3CAAC", "D9CA92", "DFCA77", "E4CA5A", "E9C936" });
        codemap5Y.Add(new List<string> { "B8AF94", "BEAF79", "C5AF5E", "CAAE41", "CEAE0F" });
        codemap5Y.Add(new List<string> { "9D957A", "A49561", "AA9445", "AF9323" });
        codemap5Y.Add(new List<string> { "837B62", "8A7A49", "90792B" });
        codemap5Y.Add(new List<string> { "696149", "706031", "745F15" });
        codemap5Y.Add(new List<string> { "504834", "55471E" });
        codemap5Y.Add(new List<string> { "383120", "3E3002" });
        codemap5Y.Add(new List<string> { "241C07" });

        codemap75Y.Add(new List<string> { "ECE7C4", "F0E8A8", "F4E88C", "F8E86D", "FBE94C", "FEE90C" });
        codemap75Y.Add(new List<string> { "D0CBAC", "D5CC91", "D9CC75", "DCCD56", "DFCD30" });
        codemap75Y.Add(new List<string> { "B5B093", "BAB178", "BEB15D", "C1B13D" });
        codemap75Y.Add(new List<string> { "9B967A", "9F9660", "A39743", "A6971D" });
        codemap75Y.Add(new List<string> { "817C62", "857C48", "897C29" });
        codemap75Y.Add(new List<string> { "676249", "6D6230", "6E6211" });
        codemap75Y.Add(new List<string> { "4D4934", "51491D" });
        codemap75Y.Add(new List<string> { "363220", "3A3200" });
        codemap75Y.Add(new List<string> { "211D09" });

        codemap10Y.Add(new List<string> { "E9E7C4", "ECE9A8", "EEEA8C", "F0EB6C", "F2EC4A" });
        codemap10Y.Add(new List<string> { "CECCAB", "D0CD91", "D2CE75", "D4CF56", "D6D02F" });
        codemap10Y.Add(new List<string> { "B3B193", "B5B279", "B7B35D", "B9B43D" });
        codemap10Y.Add(new List<string> { "98977A", "9A9861", "9C9943", "9E991D" });
        codemap10Y.Add(new List<string> { "7E7C63", "807E48", "827E2A" });
        codemap10Y.Add(new List<string> { "62634B", "666430", "676411" });
        codemap10Y.Add(new List<string> { "4A4A35", "4C4A1D" });
        codemap10Y.Add(new List<string> { "333222", "353304" });
        codemap10Y.Add(new List<string> { "1E1E0C" });

        codemap25GY.Add(new List<string> { "E6E8C5", "E6EBA9", "E5ED8E", "E4EE6F", "E3F04E", "E2F10D" });
        codemap25GY.Add(new List<string> { "CBCDAC", "CACF93", "C9D178", "C8D35C", "C7D435" });
        codemap25GY.Add(new List<string> { "AFB294", "AEB47B", "AEB662", "ADB743", "ACB90B" });
        codemap25GY.Add(new List<string> { "94987C", "949A63", "939B48", "929D24" });
        codemap25GY.Add(new List<string> { "7A7D64", "797F4C", "788130" });
        codemap25GY.Add(new List<string> { "60644C", "5F6534", "5E6717" });
        codemap25GY.Add(new List<string> { "474A36", "464C20" });
        codemap25GY.Add(new List<string> { "303324", "2F350C" });
        codemap25GY.Add(new List<string> { "1B1E0F" });

        codemap5GY.Add(new List<string> { "E3E9C6", "E0ECAC", "DCEF92", "D8F276", "D4F457", "D0F629" });
        codemap5GY.Add(new List<string> { "C7CEAE", "C4D097", "C0D37E", "BCD564", "B8D844" });
        codemap5GY.Add(new List<string> { "ACB396", "A8B67F", "A4B868", "A0BA4D", "9CBC27" });
        codemap5GY.Add(new List<string> { "91987E", "8D9B67", "899E50", "84A032" });
        codemap5GY.Add(new List<string> { "777E67", "738151", "6E8338", "6A8517" });
        codemap5GY.Add(new List<string> { "5D644E", "596738", "546920" });
        codemap5GY.Add(new List<string> { "454B38", "404D25", "3B4F0B" });
        codemap5GY.Add(new List<string> { "2E3426", "293613" });
        codemap5GY.Add(new List<string> { "191F12" });

        codemap75GY.Add(new List<string> { "DBEBCB", "D2EFB6", "C9F39F", "C0F688", "B6FA6F", "ACFC53", "A1FF2C" });
        codemap75GY.Add(new List<string> { "C0CFB3", "B7D39F", "AFD78B", "A6DA75", "9CDD5E", "92E03F" });
        codemap75GY.Add(new List<string> { "A6B49B", "9DB888", "95BB75", "8CBE60", "81C146", "76C425" });
        codemap75GY.Add(new List<string> { "8C9A81", "839D70", "7AA05D", "70A346", "65A62B" });
        codemap75GY.Add(new List<string> { "727F6A", "6A8259", "608645", "56882F", "4A8B0A" });
        codemap75GY.Add(new List<string> { "596552", "4F6841", "456B2E", "3A6E13" });
        codemap75GY.Add(new List<string> { "414C3C", "384F2C", "2E511B" });
        codemap75GY.Add(new List<string> { "2B3428", "22371A", "2E511B" });
        codemap75GY.Add(new List<string> { "161F15", "062302" });

        codemap10GY.Add(new List<string> { "D5ECD1", "C8F1BF", "B7F6AC", "A9FA9C", "98FE89", "86FF78", "6FFF63", "50FF4B", "10FF29" });
        codemap10GY.Add(new List<string> { "BBD0B7", "AED5A8", "92D997", "92DD88", "81E076", "6EE463", "55E74F", "2FEA36" });
        codemap10GY.Add(new List<string> { "A1B59F", "95B98F", "88BD81", "79C171", "67C460", "51C84D", "33CA37" });
        codemap10GY.Add(new List<string> { "879A86", "7B9E77", "6DA268", "5DA658", "4AA948", "2EAC34" });
        codemap10GY.Add(new List<string> { "6F806E", "628460", "548751", "428B41", "298E30" });
        codemap10GY.Add(new List<string> { "566555", "496948", "386D39", "227029" });
        codemap10GY.Add(new List<string> { "3E4C3F", "315033", "205326" });
        codemap10GY.Add(new List<string> { "29342C", "1C3820", "033A16" });
        codemap10GY.Add(new List<string> { "141F17" });

        codemap25G.Add(new List<string> { "D0EDD6", "BEF2CA", "A7F8BC", "91FCB1", "76FFA6", "54FF9B" });
        codemap25G.Add(new List<string> { "B7D1BD", "A6DBA6", "91DBA6", "7CDF9C", "62E391", "35E786" });
        codemap25G.Add(new List<string> { "9DB6A3", "8CBE99", "7ABF8F", "63C385", "43C77A" });
        codemap25G.Add(new List<string> { "849B8A", "72A080", "5FA476", "45A86D", "14AB64" });
        codemap25G.Add(new List<string> { "6B8071", "5A8568", "45895F", "228C56" });
        codemap25G.Add(new List<string> { "536659", "406A50", "246E48" });
        codemap25G.Add(new List<string> { "3C4C41", "29503A", "065433" });
        codemap25G.Add(new List<string> { "27352C", "153826" });
        codemap25G.Add(new List<string> { "132018" });

        codemap5G.Add(new List<string> { "CEEDDA", "B8F3D2", "9CF9CA", "7EFEC3", "59FFBD" });
        codemap5G.Add(new List<string> { "B4D1C0", "A0D6B9", "87DCB2", "6BE0AB", "43E4A6" });
        codemap5G.Add(new List<string> { "9BB6A7", "87BBA0", "71BF99", "54C494", "19C88E" });
        codemap5G.Add(new List<string> { "829B8D", "6CA086", "55A480", "31A87B" });
        codemap5G.Add(new List<string> { "698074", "55856E", "3B8968" });
        codemap5G.Add(new List<string> { "51665B", "3B6B55", "186F50" });
        codemap5G.Add(new List<string> { "3A4D43", "24513E" });
        codemap5G.Add(new List<string> { "26352E", "113829" });
        codemap5G.Add(new List<string> { "112019" });

        codemap75G.Add(new List<string> { "CCEDDE", "B4F3D8", "96F9D2", "75FECD", "48FFC9" });
        codemap75G.Add(new List<string> { "B3D1C3", "9DD6BE", "82DCB9", "64E0B5", "2FE4B1" });
        codemap75G.Add(new List<string> { "9AB6A9", "84BBA4", "6CBFA0", "4AC49C" });
        codemap75G.Add(new List<string> { "819B8F", "69A08A", "4FA587", "23A883" });
        codemap75G.Add(new List<string> { "688076", "528572", "34896E" });
        codemap75G.Add(new List<string> { "50665D", "386B59", "056F56" });
        codemap75G.Add(new List<string> { "394D45", "205142" });
        codemap75G.Add(new List<string> { "25352F", "0D382C" });
        codemap75G.Add(new List<string> { "10201B" });

        codemap10G.Add(new List<string> { "CBEDE0", "B2F3DD", "92F9D9", "6CFED6", "33FFD4" });
        codemap10G.Add(new List<string> { "B2D1C6", "9BD7C2", "7EDCC0", "5CE0BD", "14E4BB" });
        codemap10G.Add(new List<string> { "99B6AC", "82BBA9", "67C0A6", "40C4A4" });
        codemap10G.Add(new List<string> { "809B92", "66A08F", "4AA58D", "0BA98B" });
        codemap10G.Add(new List<string> { "678078", "4F8576", "2F8974" });
        codemap10G.Add(new List<string> { "4F665F", "356B5D" });
        codemap10G.Add(new List<string> { "384D47", "1B5145" });
        codemap10G.Add(new List<string> { "243530", "07392F" });
        codemap10G.Add(new List<string> { "0F201C" });

        codemap25BG.Add(new List<string> { "CBEDE3", "D0F3E1", "8EF9E0", "64FEDF", "0FFFDF" });
        codemap25BG.Add(new List<string> { "B1D1C8", "98D7C6", "7CDBC6", "55E0C5" });
        codemap25BG.Add(new List<string> { "98B6AD", "80BBAD", "64BFAC", "36C4AC" });
        codemap25BG.Add(new List<string> { "7F9B94", "64A093", "46A593" });
        codemap25BG.Add(new List<string> { "66807A", "4D857A", "2A897A" });
        codemap25BG.Add(new List<string> { "4E6661", "316B62" });
        codemap25BG.Add(new List<string> { "374D49", "17514A" });
        codemap25BG.Add(new List<string> { "233532", "003833" });
        codemap25BG.Add(new List<string> { "0E201E" });

        codemap5BG.Add(new List<string> { "CBEDE5", "AEF3E6", "8BF9E8", "5AFEE9" });
        codemap5BG.Add(new List<string> { "B1D1CB", "97D6CC", "79DBCD", "4BE0D0" });
        codemap5BG.Add(new List<string> { "97B6B0", "7EBBB2", "61BEB4", "29C4B6" });
        codemap5BG.Add(new List<string> { "7E9B97", "63A099", "3FA49B" });
        codemap5BG.Add(new List<string> { "66807D", "4B8580", "228982" });
        codemap5BG.Add(new List<string> { "4D6664", "2F6B67" });
        codemap5BG.Add(new List<string> { "364D4B", "11514F" });
        codemap5BG.Add(new List<string> { "223534" });
        codemap5BG.Add(new List<string> { "0C2020" });

        codemap75BG.Add(new List<string> { "CBECE9", "AEF2ED", "88F8F1", "4FFDE6" });
        codemap75BG.Add(new List<string> { "B2D0CE", "97D6D1", "75DBD6", "45DFDA" });
        codemap75BG.Add(new List<string> { "98B5B3", "7DBAB7", "5FBFBB", "22C3C0" });
        codemap75BG.Add(new List<string> { "7E9B99", "62A09E", "3DA4A3" });
        codemap75BG.Add(new List<string> { "658080", "4B8484", "1B8989" });
        codemap75BG.Add(new List<string> { "4C6666", "2E6A6B" });
        codemap75BG.Add(new List<string> { "354D4D", "0D5153" });
        codemap75BG.Add(new List<string> { "223536" });
        codemap75BG.Add(new List<string> { "0B2022" });

        codemap10BG.Add(new List<string> { "CDECEB", "AEF2F2", "87F7F9" });
        codemap10BG.Add(new List<string> { "B3D0D0", "97D5D6", "76DADD", "42DAE4" });
        codemap10BG.Add(new List<string> { "99B5B6", "7EBABC", "5FBEC3", "1DC2CA" });
        codemap10BG.Add(new List<string> { "719A9B", "649FA3", "3CA3AA" });
        codemap10BG.Add(new List<string> { "668082", "4B8489", "1A8891" });
        codemap10BG.Add(new List<string> { "4D666B", "2E6A70" });
        codemap10BG.Add(new List<string> { "354C50", "0D5058" });
        codemap10BG.Add(new List<string> { "21353B" });
        codemap10BG.Add(new List<string> { "0A2024" });

        codemap25B.Add(new List<string> { "CFEBED", "B0F1F8" });
        codemap25B.Add(new List<string> { "B5CFD2", "99D4DB", "78D9E4", "45DDEE" });
        codemap25B.Add(new List<string> { "9AB4B7", "80B9C0", "62BDC9", "28C0D3" });
        codemap25B.Add(new List<string> { "809A9D", "679EA6", "40A2B0" });
        codemap25B.Add(new List<string> { "677F83", "4D838D", "208796" });
        codemap25B.Add(new List<string> { "4D656A", "306974" });
        codemap25B.Add(new List<string> { "354C52", "0E505C" });
        codemap25B.Add(new List<string> { "213439" });
        codemap25B.Add(new List<string> { "0A2025" });

        codemap5B.Add(new List<string> { "D2EAEF", "B4EFFD" });
        codemap5B.Add(new List<string> { "B8CED3", "9ED3DF", "80D7EB", "50DBF9" });
        codemap5B.Add(new List<string> { "9DB3B9", "85B7C4", "68BBCF", "3CBEDB" });
        codemap5B.Add(new List<string> { "83999F", "6C9DA9", "4BA0B6" });
        codemap5B.Add(new List<string> { "697F85", "518290", "2D859C" });
        codemap5B.Add(new List<string> { "4F656C", "356877" });
        codemap5B.Add(new List<string> { "364C53", "144F5F" });
        codemap5B.Add(new List<string> { "22343B" });
        codemap5B.Add(new List<string> { "0A1F27" });

        codemap75B.Add(new List<string> { "D6E9EF", "BAEEFF" });
        codemap75B.Add(new List<string> { "BBCDD4", "A4D1E1", "89D5EF", "60D8FF" });
        codemap75B.Add(new List<string> { "A0B3BA", "8AB6C6", "72B9D3", "4FBCE1" });
        codemap75B.Add(new List<string> { "8598A0", "729BAC", "579EB9", "2EA0C6" });
        codemap75B.Add(new List<string> { "6B7E86", "578193", "3B839F" });
        codemap75B.Add(new List<string> { "51646D", "3C667A", "186986" });
        codemap75B.Add(new List<string> { "374B55", "1D4E62" });
        codemap75B.Add(new List<string> { "23343C", "043648" });
        codemap75B.Add(new List<string> { "0C1F28" });

        codemap10B.Add(new List<string> { "D8E8F0", "C0ECFF" });
        codemap10B.Add(new List<string> { "BECCD4", "ABCFE4", "95D2F3", "75D5FF" });
        codemap10B.Add(new List<string> { "A3B2BA", "92B4C8", "7EB7D6", "63B9E4", "35BBF4" });
        codemap10B.Add(new List<string> { "8897A0", "789AAD", "639CBC", "479EC9" });
        codemap10B.Add(new List<string> { "6E7D87", "5D7F94", "4881A1", "2383AF" });
        codemap10B.Add(new List<string> { "54636E", "42657B", "2A6788" });
        codemap10B.Add(new List<string> { "3A4B56", "264C64" });
        codemap10B.Add(new List<string> { "25333E", "0F354A" });
        codemap10B.Add(new List<string> { "0F1E2A" });

        codemap25PB.Add(new List<string> { "DCE7F0" });
        codemap25PB.Add(new List<string> { "C1CBD5", "B3CDE5", "A2CFF5" });
        codemap25PB.Add(new List<string> { "A6B1BB", "99B2C9", "8BB4D8", "78B5E7", "5EB7F8" });
        codemap25PB.Add(new List<string> { "8B96A1", "8098AE", "7099BD", "5F9ACC", "459BDA" });
        codemap25PB.Add(new List<string> { "717C88", "8098AE", "7099BD", "5F9ACC", "1C81BF" });
        codemap25PB.Add(new List<string> { "57626F", "4A647C", "3A6489", "1C6597" });
        codemap25PB.Add(new List<string> { "3D4A57", "2F4B65", "184B72" });
        codemap25PB.Add(new List<string> { "28323F", "19334B" });
        codemap25PB.Add(new List<string> { "121E2A" });

        codemap5PB.Add(new List<string> { "DFE6F0" });
        codemap5PB.Add(new List<string> { "C4CDBD", "BACCE5", "ADCCF7" });
        codemap5PB.Add(new List<string> { "A9B0BB", "A0B1C9", "95B1D8", "89B2E8", "78B2F9" });
        codemap5PB.Add(new List<string> { "8E95A1", "8696AF", "7C96BE", "7097CD", "6097DB", "4898EB", "1798FB" });
        codemap5PB.Add(new List<string> { "757B88", "6C7C96", "627CA4", "557CB2", "437DC0", "267DCE" });
        codemap5PB.Add(new List<string> { "5B616F", "52627D", "47628A", "376298", "2062A5" });
        codemap5PB.Add(new List<string> { "414958", "384966", "2A4973", "15497F" });
        codemap5PB.Add(new List<string> { "2B313F", "22324C", "113259" });
        codemap5PB.Add(new List<string> { "161D2B", "0B1D36" });

        codemap75PB.Add(new List<string> { "E2E5F0" });
        codemap75PB.Add(new List<string> { "C7CAD5", "C2C9E5", "BCC9F6" });
        codemap75PB.Add(new List<string> { "ACAFBB", "A8AEC9", "A3AED9", "9EADE8", "98ABF9" });
        codemap75PB.Add(new List<string> { "9294A1", "8E94AF", "8A93BE", "8592CB", "8091D9", "798FE9", "728EF9" });
        codemap75PB.Add(new List<string> { "787A88", "747A95", "7079A4", "6C77B1", "6776BE", "6175CC", "5A73DA", "5371E8", "4D6DF7", "486AFF" });
        codemap75PB.Add(new List<string> { "5E6070", "5B5F7D", "575E8A", "535D97", "4D5BA4", "485AB1", "4457BD", "4054CA", "3E50D7", "3D4AE5", "3E46EE", "3F3FFA", "4137FF" });
        codemap75PB.Add(new List<string> { "464758", "424666", "3E4573", "3A437F", "36418B", "333E97", "323AA3", "3235AF", "332FB9", "3529C2", "381FCD", "3B0ED7" });
        codemap75PB.Add(new List<string> { "2F3040", "2C2F4C", "292D58", "262B64", "242870", "25247A", "261E84", "29168D", "2C0897" });
        codemap75PB.Add(new List<string> { "1B1B2B", "181A36", "161741", "17134B", "190E54", "1C055C" });

        codemap10PB.Add(new List<string> { "E5E4EF", "E3E3FF" });
        codemap10PB.Add(new List<string> { "CAC9D4", "C9C8E3", "C8C5F4", "C7C3FF" });
        codemap10PB.Add(new List<string> { "AFAEBA", "AEADC9", "AEABD7", "ADA9E5", "ADA6F5", "ADA3FF" });
        codemap10PB.Add(new List<string> { "9594A1", "9592AE", "9490BC", "948CD5", "948CD5", "9489E4", "9586F2", "9681FF" });
        codemap10PB.Add(new List<string> { "7B7988", "7B7894", "7C75A2", "7C73AF", "7C70BA", "7D6DC7", "7E6AD4", "7F66E0", "8162EB", "835DF7", "8756FF" });
        codemap10PB.Add(new List<string> { "625F70", "625D7C", "635B88", "645894", "6456A0", "6652AC", "674EB7", "6A49C1", "6D43CC", "713CD8", "7434E0", "782AEA", "7C17F4" });
        codemap10PB.Add(new List<string> { "4A4658", "4A4465", "4C4170", "4D3E7B", "4F3A87", "513691", "54309C", "5828A6", "5B1FAF", "5F10B8" });
        codemap10PB.Add(new List<string> { "332F3F", "342D4B", "382A55", "382660", "3A216A", "2D1B73", "41127C" });
        codemap10PB.Add(new List<string> { "1F1A2B", "211735", "23143E", "250F46", "3A29064F216A" });

        codemap25P.Add(new List<string> { "E7E4EF", "E9E1FF" });
        codemap25P.Add(new List<string> { "CCC9D3", "CEC6E2", "D1C3F0", "D4C0FF" });
        codemap25P.Add(new List<string> { "B1AEBA", "B4ABC7", "B7A8D3", "BAA5E0", "BEA1EE", "C29DFB" });
        codemap25P.Add(new List<string> { "9793A0", "9A90AC", "9D8EB9", "A18AC5", "A487D1", "A983DC", "AD7EE8", "B379F4", "B773FE" });
        codemap25P.Add(new List<string> { "7E7987", "817693", "84739F", "886FAB", "8C6CB5", "9068C0", "9563CA", "995DD4", "9E57DE", "A350E9", "A847F3", "AD3BFE", "B32CFF" });
        codemap25P.Add(new List<string> { "655E6F", "685B7A", "6C5985", "70558F", "74519A", "784CA4", "7D46AE", "823FB7", "8737C1", "8C2BCB", "901ED3" });
        codemap25P.Add(new List<string> { "4D4557", "514263", "553E6D", "593A77", "5E3481", "622E8A", "672693", "6C1A9C", "7104A5" });
        codemap25P.Add(new List<string> { "362E3E", "392B48", "3D2851", "42225B", "471C64", "4C126C" });
        codemap25P.Add(new List<string> { "22192A", "261533", "2A113A", "2E0942" });

        codemap5P.Add(new List<string> { "E8E4EE", "EEE0FD" });
        codemap5P.Add(new List<string> { "CDC8D2", "D3C5DF", "D8C1EC", "DFBDF9", "E5B8FF" });
        codemap5P.Add(new List<string> { "B4ADB9", "B9AAC4", "BEA6CF", "C4A4DB", "CA9EE7", "D099F2", "D794FD" });
        codemap5P.Add(new List<string> { "9A929F", "9F8FAA", "A58BB5", "AA88BF", "B083CA", "B67FD4", "BC79DF", "C273E9", "C86CF3", "CF64FE" });
        codemap5P.Add(new List<string> { "807885", "867590", "8C719B", "926DA5", "9768AF", "9D63B9", "A35DC2", "A967CB", "AF4FD4", "B546DD", "BB39E7", "C228F0", "C801F9" });
        codemap5P.Add(new List<string> { "685E6D", "6D5A78", "735681", "78528A", "7E4D94", "84479D", "8A40A6", "9037AF", "962CB8", "9C1AC1" });
        codemap5P.Add(new List<string> { "504456", "564060", "5B3C69", "613772", "67307B", "6D2884", "731D8C", "790894" });
        codemap5P.Add(new List<string> { "382E3D", "3E2A46", "42264D", "491F57", "4E175F", "540967" });
        codemap5P.Add(new List<string> { "241829", "291431", "2E0E38", "33043F" });

        codemap75P.Add(new List<string> { "ECE3EB", "F8DEF6", "FFD8FF" });
        codemap75P.Add(new List<string> { "D0C7D0", "DBC3D9", "E3BFE2", "EEB8ED", "F8B3F7", "FFACFF" });
        codemap75P.Add(new List<string> { "B7ACB6", "C0A8BF", "C8A4C8", "D19FD1", "DA99DB", "E293E3", "EB8CED", "F485F6", "FD7CFF" });
        codemap75P.Add(new List<string> { "9D919D", "A58EA5", "AE89AF", "B684B7", "BE7FC0", "C679C8", "CE72D1", "D66BD9", "DD62E2", "E656EC", "EF47F5", "F734FE" });
        codemap75P.Add(new List<string> { "837783", "8B738C", "946E95", "9C699E", "A364A6", "AA5DAE", "B256B6", "B94EBE", "C043C7", "C836CF", "D020D8" });
        codemap75P.Add(new List<string> { "6B5D6B", "725974", "7A547C", "814F84", "88498C", "8F4294", "96399C", "9E2CA5", "A51AAD" });
        codemap75P.Add(new List<string> { "534454", "5A3F5C", "613A64", "69336D", "6F2C75", "76217D", "7D0F85" });
        codemap75P.Add(new List<string> { "3A2D3C", "412943", "47244A", "4E1C53", "54125A" });
        codemap75P.Add(new List<string> { "261828", "2C132F", "310C35" });

        codemap10P.Add(new List<string> { "EDE3E9", "FCDDF1", "FFD7F9" });
        codemap10P.Add(new List<string> { "D2C7CE", "DEC2D5", "EABDDD", "F7B6E5", "FFB0EC", "FFA9F4", "FFA0FD" });
        codemap10P.Add(new List<string> { "B9ACB4", "C4A7BB", "CFA2C2", "DA9CC9", "E496D1", "ED90D7", "F888DF", "FF7EE7", "FF74EE", "FF68F6", "FF58FF" });
        codemap10P.Add(new List<string> { "9F919B", "A98DA2", "B487A9", "BD82AF", "C77CB6", "D075BD", "DA6CC4", "E364CG", "EB5AD2", "F54BDA", "FE39E1", "FF1DE8" });
        codemap10P.Add(new List<string> { "857781", "8F7288", "9A6D8F", "A46696", "AC609D", "B559A4", "BD50AA", "C646B1", "CE39B8", "D724BF" });
        codemap10P.Add(new List<string> { "6D5C69", "775770", "805277", "884C7D", "904584", "983D8B", "A03291", "A82398" });
        codemap10P.Add(new List<string> { "554351", "5E3E59", "663860", "6F3067", "76376E", "7F1875" });
        codemap10P.Add(new List<string> { "3C2D3A", "432841", "4A2347", "521A4E", "590D55" });
        codemap10P.Add(new List<string> { "271721", "2E122D", "330B33" });

        codemap25RP.Add(new List<string> { "EFE2E8", "FFDCEC", "FFD5F1" });
        codemap25RP.Add(new List<string> { "D3C7CC", "E2C1D1", "F1BBD5", "FFB4DA", "FFADDF", "FFA4E4", "FF9BEA" });
        codemap25RP.Add(new List<string> { "BAACB2", "C7A6B6", "D5A0BB", "E19AC0", "EC93C4", "F88CCA", "FF83CE", "FF78D4", "FF6BDA", "FF5BE0" });
        codemap25RP.Add(new List<string> { "A19199", "AD8C9D", "B986A2", "C480A6", "CF79AB", "DA71B0", "E568B5", "EF5DBA", "F951BF", "FF3FC4", "FF29C9" });
        codemap25RP.Add(new List<string> { "87777F", "937184", "9F6B89", "AB648E", "B45D92", "B34597", "C84A9C", "D13EA1", "DA2CA6", "E307AB" });
        codemap25RP.Add(new List<string> { "6F5C66", "7A576B", "855070", "8F4975", "98417A", "A1377F", "AA2A84", "B31489" });
        codemap25RP.Add(new List<string> { "57434E", "623D54", "6B3659", "752D5E", "7E2263", "870E69" });
        codemap25RP.Add(new List<string> { "3E2C38", "46273D", "4E2142", "571648", "5F034D" });
        codemap25RP.Add(new List<string> { "291726", "30112B", "360931" });

        codemap5RP.Add(new List<string> { "F0E2E6", "FFDBE7", "FFD4E8" });
        codemap5RP.Add(new List<string> { "D5C7CB", "E5C1CB", "F6BACD", "FFB2CE", "FFABD0", "FFA0D2" });
        codemap5RP.Add(new List<string> { "BCABB0", "CAA6B1", "DA9FB3", "E898B4", "F491B6", "FF88B8", "FF7EBA", "FF72BD", "FF64C0" });
        codemap5RP.Add(new List<string> { "A39096", "B08B97", "BF8499", "CB7E9B", "D6779C", "E26E9F", "EE64A1", "F958A4", "FF49A6", "FF2FAA", "FF04AC" });
        codemap5RP.Add(new List<string> { "89767C", "97707E", "A46A80", "B16282", "BC5A84", "C75087", "C24489", "DC358C", "E61C8E" });
        codemap5RP.Add(new List<string> { "715C63", "7D5665", "8A4F68", "95476A", "9E3F6D", "A93270", "B22273" });
        codemap5RP.Add(new List<string> { "58424B", "653C4E", "703451", "7A2B55", "841D58" });
        codemap5RP.Add(new List<string> { "402C36", "492639", "51203D", "5B1442" });
        codemap5RP.Add(new List<string> { "2A1624", "321028", "38072D" });

        codemap75RP.Add(new List<string> { "F1E2E4", "FFDBE2", "FFD3E1" });
        codemap75RP.Add(new List<string> { "D6C6C9", "E8C0C7", "F9B9C6", "FFB1C6", "FFA9C5", "FF9FC5" });
        codemap75RP.Add(new List<string> { "BDABAE", "CCA6AD", "DC9FAC", "EB98AC", "F890AB", "FF87AB", "FF7CAB", "FF6FAC" });
        codemap75RP.Add(new List<string> { "A39094", "B28B93", "C28492", "CE7E92", "DB7692", "E76D92", "F36293", "FF5493", "FF4494", "FF2896" });
        codemap75RP.Add(new List<string> { "8A767A", "997079", "A76979", "B46179", "C05979", "CC4D7A", "D6427B", "E1307C", "EB117D" });
        codemap75RP.Add(new List<string> { "725B61", "805561", "8D4E61", "994661", "A33D62", "AD3063", "B81C64" });
        codemap75RP.Add(new List<string> { "5A4249", "673B4A", "72344B", "7E294C", "881A4E" });
        codemap75RP.Add(new List<string> { "402C34", "4A2636", "531F39", "5D123C" });
        codemap75RP.Add(new List<string> { "2B1622", "330F26", "3B0429" });

        codemap10RP.Add(new List<string> { "F2E2E3", "FFDBDE", "FFD3D9" });
        codemap10RP.Add(new List<string> { "D7C6C8", "EAC0C3", "FBB9C0", "EEB1BC", "FFA9B8" });
        codemap10RP.Add(new List<string> { "BEABAC", "CEA5A9", "DE9FA6", "EE97A2", "FC909F", "FF859C", "FF7B9A", "FF6E97" });
        codemap10RP.Add(new List<string> { "A49092", "B48A8F", "C4848C", "D07D89", "DE7587", "EB6B84", "F76183", "FF5280", "FF417" });
        codemap10RP.Add(new List<string> { "8B7678", "9B7075", "A96973", "B76170", "C3586E", "D04C6D", "DA406C", "E62D6A" });
        codemap10RP.Add(new List<string> { "735B5F", "81555C", "8E4E5A", "9B4559", "A63B58", "B12D57", "BC1756" });
        codemap10RP.Add(new List<string> { "5B4246", "683B45", "743343", "802843", "8B1742" });
        codemap10RP.Add(new List<string> { "412C32", "4C2533", "551E34", "5F1036" });
        codemap10RP.Add(new List<string> { "2C1620", "350F23", "3C0325" });

        hval = 0;

        codemap = codemap25R;
        MakeColormap();

        // hueslider.onValueChanged.AddListener(HueValue);

    }

    public void OnClose()
    {
        targetObject = null;
        isEdit = false;

        if (_bbox)
        {
            Destroy(_bbox);
            _bbox = null;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isEdit)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }

                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit = new RaycastHit();

                if (Physics.Raycast(ray, out hit))
                {
                    int layer = LayerMask.NameToLayer("Building");
                    if (hit.collider.gameObject.layer == layer)
                    {
                        Debug.Log(hit.collider.gameObject.name);
                        targetObject = hit.collider.gameObject;
                        if (_bbox == null)
                        {
                            _bbox = Instantiate(bbox);

                            MeshFilter mf = _bbox.GetComponent<MeshFilter>();
                            mf.mesh.SetIndices(mf.mesh.GetIndices(0), MeshTopology.LineStrip, 0);
                            // MeshRenderer mr = _bbox.GetComponent<MeshRenderer>();
                            // mr.material.color = Color.white;
                        }
                        var mesh = targetObject.GetComponent<MeshFilter>().mesh;
                        mesh.RecalculateBounds();
                        var bounds = mesh.bounds;

                        _bbox.transform.localPosition = bounds.center;
                        _bbox.transform.localScale = new Vector3(bounds.size.x, bounds.size.y, bounds.size.z);

                        cachedColor = GetBuildingColor();
                        rgbColorSelectUI.Color = GetBuildingColor();
                    }
                }
                else
                {
                    Debug.Log("No hit");
                    targetObject = null;
                }
            }
        }

        if (cachedColor != rgbColorSelectUI.Color)
        {
            cachedColor = rgbColorSelectUI.Color;
            UpdateBuildingColor();
        }

        colorSample.color = cachedColor;
    }

    private void OnEnable()
    {
        isEdit = true;
    }

    private void OnDisable()
    {
        isEdit = false;
    }

    void MakeColormap()
    {
        int n = 0;
        for (int v = 0; v < 9; v++)
        {
            List<string> cmp = codemap[v];
            for (int s = 0; s < 14; s++)
            {
                Color color;
                GameObject btnimg = mapButtons.transform.GetChild(n++).gameObject;
                if (s < cmp.Count)
                {
                    string code = cmp[s];
                    if (ColorUtility.TryParseHtmlString("#" + code, out color))
                    {
                        Image ig = btnimg.GetComponent<Image>();
                        ig.color = color;
                        ig.enabled = true;
                        Button btn = btnimg.GetComponent<Button>();
                        btn.enabled = true;
                    }
                }
                else
                {
                    Image ig = btnimg.GetComponent<Image>();
                    ig.enabled = false;
                    Button btn = btnimg.GetComponent<Button>();
                    btn.enabled = false;
                }
            }
        }
    }

    Material[] _oldMat;
    private Color cachedColor;

    public void SelectColor(int n)
    {
        Color col = Color.white;
        for (int i = 0; i < mapButtons.transform.childCount; i++)
        {
            GameObject btnimg = mapButtons.transform.GetChild(i).gameObject;
            if (i == n)
            {
                btnimg.GetComponent<Outline>().enabled = true;
                col = btnimg.GetComponent<Image>().color;
            }
            else
            {
                btnimg.GetComponent<Outline>().enabled = false;
            }
        }

        col.a = 1f;
        rgbColorSelectUI.Color = col;
        cachedColor = col;

        UpdateBuildingColor();
    }

    private Color GetBuildingColor()
    {
        if (targetObject)
        {
            return targetObject.GetComponent<Renderer>().sharedMaterial.color * 2f;
        }
        return Color.white;
    }

    private void UpdateBuildingColor()
    {
        if (targetObject)
        {
            var col = rgbColorSelectUI.Color;

            // bool hasColor = false;
            Material[] materials = targetObject.GetComponent<Renderer>().sharedMaterials;

            //_oldMat = new Material[materials.Length];
            //int i = 0;
            //foreach (var mat in materials)
            //{
            //    _oldMat[i] = materials[i];
            //    i++;
            //}

            foreach (var mat in materials)
            {
                // mat.SetColor("_BaseColor", col / 2f);
                mat.color = col / 2f;
                // hasColor = true;
            }
        }
    }

    public void CancelColor()
    {
        cachedColor = Color.white;
        rgbColorSelectUI.Color = Color.white;
        UpdateBuildingColor();
    }

    public void HueValue(float val)
    {
        int ival = (int)hueslider.value;
        Debug.Log(ival);
        switch (ival)
        {
            case 0:
                codemap = codemap25R;
                break;
        }

        hval = ival;
        switch (hval)
        {
            case 0:
                codemap = codemap25R; break;
            case 1:
                codemap = codemap5R; break;
            case 2:
                codemap = codemap75R; break;
            case 3:
                codemap = codemap10R; break;
            case 4:
                codemap = codemap25YR; break;
            case 5:
                codemap = codemap5YR; break;
            case 6:
                codemap = codemap75YR; break;
            case 7:
                codemap = codemap10YR; break;
            case 8:
                codemap = codemap25Y; break;
            case 9:
                codemap = codemap5Y; break;
            case 10:
                codemap = codemap75Y; break;
            case 11:
                codemap = codemap10Y; break;
            case 12:
                codemap = codemap25GY; break;
            case 13:
                codemap = codemap5GY; break;
            case 14:
                codemap = codemap75GY; break;
            case 15:
                codemap = codemap10GY; break;
            case 16:
                codemap = codemap25G; break;
            case 17:
                codemap = codemap5G; break;
            case 18:
                codemap = codemap75G; break;
            case 19:
                codemap = codemap10G; break;
            case 20:
                codemap = codemap25BG; break;
            case 21:
                codemap = codemap5BG; break;
            case 22:
                codemap = codemap75BG; break;
            case 23:
                codemap = codemap10BG; break;
            case 24:
                codemap = codemap25B; break;
            case 25:
                codemap = codemap5B; break;
            case 26:
                codemap = codemap75B; break;
            case 27:
                codemap = codemap10B; break;
            case 28:
                codemap = codemap25PB; break;
            case 29:
                codemap = codemap5PB; break;
            case 30:
                codemap = codemap75PB; break;
            case 31:
                codemap = codemap10PB; break;
            case 32:
                codemap = codemap25P; break;
            case 33:
                codemap = codemap5P; break;
            case 34:
                codemap = codemap75P; break;
            case 35:
                codemap = codemap10P; break;
            case 36:
                codemap = codemap25RP; break;
            case 37:
                codemap = codemap5RP; break;
            case 38:
                codemap = codemap75RP; break;
            case 39:
                codemap = codemap10RP; break;
        }

        MakeColormap();
    }
}
