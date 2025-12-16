namespace find_and_make;

using System.Collections.Generic;
using System.Xml.Serialization;

//
// 루트: <result> ... </result>
//
[XmlRoot("result")]
public class RestaurantXmlResult
{
    [XmlElement("header")]
    public RestaurantHeader Header { get; set; } = new();

    [XmlElement("body")]
    public RestaurantBody Body { get; set; } = new();
}

//
// <header> 안쪽
//
public class RestaurantHeader
{
    [XmlElement("columns")]
    public RestaurantColumns Columns { get; set; } = new();

    [XmlElement("paging")]
    public RestaurantPaging Paging { get; set; } = new();

    [XmlElement("process")]
    public RestaurantProcess Process { get; set; } = new();
}

//
// <columns> : 컬럼 설명 (한국어 라벨들)
// 굳이 안 써도 되지만, 그대로 매핑해 둔 버전
//
public class RestaurantColumns
{
    [XmlElement("rowNum")]
    public string RowNum { get; set; } = "";

    [XmlElement("opnSvcNm")]
    public string OpnSvcNm { get; set; } = "";

    [XmlElement("opnSvcId")]
    public string OpnSvcId { get; set; } = "";

    [XmlElement("opnSfTeamCode")]
    public string OpnSfTeamCode { get; set; } = "";

    [XmlElement("mgtNo")]
    public string MgtNo { get; set; } = "";

    [XmlElement("apvPermYmd")]
    public string ApvPermYmd { get; set; } = "";

    [XmlElement("apvCancelYmd")]
    public string ApvCancelYmd { get; set; } = "";

    [XmlElement("trdStateGbn")]
    public string TrdStateGbn { get; set; } = "";

    [XmlElement("trdStateNm")]
    public string TrdStateNm { get; set; } = "";

    [XmlElement("dtlStateGbn")]
    public string DtlStateGbn { get; set; } = "";

    [XmlElement("dtlStateNm")]
    public string DtlStateNm { get; set; } = "";

    [XmlElement("dcbYmd")]
    public string DcbYmd { get; set; } = "";

    [XmlElement("clgStdt")]
    public string ClgStdt { get; set; } = "";

    [XmlElement("clgEnddt")]
    public string ClgEnddt { get; set; } = "";

    [XmlElement("ropnYmd")]
    public string RopnYmd { get; set; } = "";

    [XmlElement("siteTel")]
    public string SiteTel { get; set; } = "";

    [XmlElement("siteArea")]
    public string SiteArea { get; set; } = "";

    [XmlElement("sitePostNo")]
    public string SitePostNo { get; set; } = "";

    [XmlElement("siteWhlAddr")]
    public string SiteWhlAddr { get; set; } = "";

    [XmlElement("rdnWhlAddr")]
    public string RdnWhlAddr { get; set; } = "";

    [XmlElement("rdnPostNo")]
    public string RdnPostNo { get; set; } = "";

    [XmlElement("bplcNm")]
    public string BplcNm { get; set; } = "";

    [XmlElement("lastModTs")]
    public string LastModTs { get; set; } = "";

    [XmlElement("updateGbn")]
    public string UpdateGbn { get; set; } = "";

    [XmlElement("updateDt")]
    public string UpdateDt { get; set; } = "";

    [XmlElement("uptaeNm")]
    public string UptaeNm { get; set; } = "";

    [XmlElement("x")]
    public string X { get; set; } = "";

    [XmlElement("y")]
    public string Y { get; set; } = "";

    [XmlElement("sntUptaeNm")]
    public string SntUptaeNm { get; set; } = "";

    [XmlElement("manEipCnt")]
    public string ManEipCnt { get; set; } = "";

    [XmlElement("wmEipCnt")]
    public string WmEipCnt { get; set; } = "";

    [XmlElement("trdpJubnSeNm")]
    public string TrdpJubnSeNm { get; set; } = "";

    [XmlElement("lvSeNm")]
    public string LvSeNm { get; set; } = "";

    [XmlElement("wtrSplyFacilSeNm")]
    public string WtrSplyFacilSeNm { get; set; } = "";

    [XmlElement("totEpNum")]
    public string TotEpNum { get; set; } = "";

    [XmlElement("hoffEpCnt")]
    public string HoffEpCnt { get; set; } = "";

    [XmlElement("fctyOwkEpCnt")]
    public string FctyOwkEpCnt { get; set; } = "";

    [XmlElement("fctySilJobEpCnt")]
    public string FctySilJobEpCnt { get; set; } = "";

    [XmlElement("fctyPdtJobEpCnt")]
    public string FctyPdtJobEpCnt { get; set; } = "";

    [XmlElement("bdngOwnSeNm")]
    public string BdngOwnSeNm { get; set; } = "";

    [XmlElement("isreAm")]
    public string IsreAm { get; set; } = "";

    [XmlElement("monAm")]
    public string MonAm { get; set; } = "";

    [XmlElement("multUsnUpsoYn")]
    public string MultUsnUpsoYn { get; set; } = "";

    [XmlElement("facilTotScp")]
    public string FacilTotScp { get; set; } = "";

    [XmlElement("jtUpsoAsgnNo")]
    public string JtUpsoAsgnNo { get; set; } = "";

    [XmlElement("jtUpsoMainEdf")]
    public string JtUpsoMainEdf { get; set; } = "";

    [XmlElement("homepage")]
    public string Homepage { get; set; } = "";
}

//
// <paging>
//
public class RestaurantPaging
{
    [XmlElement("pageIndex")]
    public int PageIndex { get; set; }

    [XmlElement("totalCount")]
    public int TotalCount { get; set; }

    [XmlElement("pageSize")]
    public int PageSize { get; set; }
}

//
// <process>
//
public class RestaurantProcess
{
    [XmlElement("code")]
    public string Code { get; set; } = "";

    [XmlElement("message")]
    public string Message { get; set; } = "";
}

//
// <body>
///   <rows>
///     <row>...</row>
///   </rows>
/// </body>
//
public class RestaurantBody
{
    [XmlArray("rows")]
    [XmlArrayItem("row")]
    public List<RestaurantRow> Rows { get; set; } = new();
}

//
// 실제 우리가 쓰게 될 한 줄 데이터: <row> ... </row>
//
public class RestaurantRow
{
    [XmlElement("rowNum")]
    public string RowNum { get; set; } = "";

    [XmlElement("opnSvcNm")]
    public string OpnSvcNm { get; set; } = "";

    [XmlElement("opnSvcId")]
    public string OpnSvcId { get; set; } = "";

    [XmlElement("opnSfTeamCode")]
    public string OpnSfTeamCode { get; set; } = "";

    [XmlElement("mgtNo")]
    public string MgtNo { get; set; } = "";

    [XmlElement("apvPermYmd")]
    public string ApvPermYmd { get; set; } = "";

    [XmlElement("apvCancelYmd")]
    public string ApvCancelYmd { get; set; } = "";

    [XmlElement("trdCode")]
    public string TrdCode { get; set; } = "";

    [XmlElement("trdCodeNm")]
    public string TrdCodeNm { get; set; } = "";

    [XmlElement("trdStateGbn")]
    public string TrdStateGbn { get; set; } = "";

    [XmlElement("trdStateNm")]
    public string TrdStateNm { get; set; } = "";

    [XmlElement("dtlStateGbn")]
    public string DtlStateGbn { get; set; } = "";

    [XmlElement("dtlStateNm")]
    public string DtlStateNm { get; set; } = "";

    [XmlElement("dcbYmd")]
    public string DcbYmd { get; set; } = "";

    [XmlElement("clgStdt")]
    public string ClgStdt { get; set; } = "";

    [XmlElement("clgEnddt")]
    public string ClgEnddt { get; set; } = "";

    [XmlElement("ropnYmd")]
    public string RopnYmd { get; set; } = "";

    [XmlElement("siteTel")]
    public string SiteTel { get; set; } = "";

    [XmlElement("siteArea")]
    public string SiteArea { get; set; } = "";

    [XmlElement("sitePostNo")]
    public string SitePostNo { get; set; } = "";

    [XmlElement("siteWhlAddr")]
    public string SiteWhlAddr { get; set; } = "";

    [XmlElement("rdnWhlAddr")]
    public string RdnWhlAddr { get; set; } = "";

    [XmlElement("rdnPostNo")]
    public string RdnPostNo { get; set; } = "";

    [XmlElement("bplcNm")]
    public string BplcNm { get; set; } = "";

    [XmlElement("lastModTs")]
    public string LastModTs { get; set; } = "";

    [XmlElement("updateGbn")]
    public string UpdateGbn { get; set; } = "";

    [XmlElement("updateDt")]
    public string UpdateDt { get; set; } = "";

    [XmlElement("uptaeNm")]
    public string UptaeNm { get; set; } = "";

    [XmlElement("x")]
    public string X { get; set; } = "";

    [XmlElement("y")]
    public string Y { get; set; } = "";

    [XmlElement("sntUptaeNm")]
    public string SntUptaeNm { get; set; } = "";

    [XmlElement("manEipCnt")]
    public string ManEipCnt { get; set; } = "";

    [XmlElement("wmEipCnt")]
    public string WmEipCnt { get; set; } = "";

    [XmlElement("trdpJubnSeNm")]
    public string TrdpJubnSeNm { get; set; } = "";

    [XmlElement("lvSeNm")]
    public string LvSeNm { get; set; } = "";

    [XmlElement("wtrSplyFacilSeNm")]
    public string WtrSplyFacilSeNm { get; set; } = "";

    [XmlElement("totEpNum")]
    public string TotEpNum { get; set; } = "";

    [XmlElement("hoffEpCnt")]
    public string HoffEpCnt { get; set; } = "";

    [XmlElement("fctyOwkEpCnt")]
    public string FctyOwkEpCnt { get; set; } = "";

    [XmlElement("fctySilJobEpCnt")]
    public string FctySilJobEpCnt { get; set; } = "";

    [XmlElement("fctyPdtJobEpCnt")]
    public string FctyPdtJobEpCnt { get; set; } = "";

    [XmlElement("bdngOwnSeNm")]
    public string BdngOwnSeNm { get; set; } = "";

    [XmlElement("isreAm")]
    public string IsreAm { get; set; } = "";

    [XmlElement("monAm")]
    public string MonAm { get; set; } = "";

    [XmlElement("multUsnUpsoYn")]
    public string MultUsnUpsoYn { get; set; } = "";

    [XmlElement("facilTotScp")]
    public string FacilTotScp { get; set; } = "";

    [XmlElement("jtUpsoAsgnNo")]
    public string JtUpsoAsgnNo { get; set; } = "";

    [XmlElement("jtUpsoMainEdf")]
    public string JtUpsoMainEdf { get; set; } = "";

    [XmlElement("homepage")]
    public string Homepage { get; set; } = "";
}
