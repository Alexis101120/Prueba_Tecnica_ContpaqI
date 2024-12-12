namespace LectorXML.DTO
{
    internal class Respuesta_Error_XML
    {
        public string Status {  get; set; }
        public string Title {  get; set; }
        public string Detail {  get; set; }
        public List<string> Errors { get; set; } = [];
    }
}
