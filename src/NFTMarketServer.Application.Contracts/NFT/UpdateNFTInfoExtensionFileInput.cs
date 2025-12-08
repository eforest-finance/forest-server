namespace NFTMarketServer.NFT
{
    public class UpdateNFTInfoExtensionFileInput : InputBase
    {
        public NFTInfoFileType Type { get; set; }
        public string FileName { get; set; }
        public byte[] File { get; set; }
    }

    public enum NFTInfoFileType
    {
        PreviewImage,
        File
    }
}