using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using TencentCloud.Common;
using TencentCloud.Iai.V20180301;
using TencentCloud.Iai.V20180301.Models;

namespace FaceDistinguishSDK.NetWork
{
    public class Client
    {
        Credential cred = new Credential
        {
            SecretId = "AKIDreIqTZcFh3Skrf6HzgM5yxqeJpDbM00w",
            SecretKey = "IYVSI2cfYHveDhAThsv6GFByc6Shv1xl"
        };

        public bool CheckFaceIdentity(Bitmap bitmap, string fileName, string[] groupIDs, out string result)
        {
            if (bitmap == null || groupIDs == null)
            {
                throw new NullReferenceException("bitmap和GroupIDs不能为null！");
            }
            string base64 = "";
            using (MemoryStream stream = new MemoryStream())
            {
                bitmap.Save(stream, ImageFormat.Jpeg);
                byte[] data = new byte[stream.Length];
                stream.Seek(0, SeekOrigin.Begin);
                stream.Read(data, 0, Convert.ToInt32(stream.Length));
                base64 = Convert.ToBase64String(data);
            }
            IaiClient client = new IaiClient(cred, "");

            SearchFacesRequest req = new SearchFacesRequest()
            {
                Image = base64,
                GroupIds = groupIDs
            };
            try
            {
                SearchFacesResponse resp = client.SearchFaces(req).
                ConfigureAwait(false).GetAwaiter().GetResult();
                result = AbstractModel.ToJsonString(resp);
            }
            catch(Exception e) {
                result = e.Message;
                return false;
            }
            return true;
        }

        public bool CreateGroup()
        {
            IaiClient client = new IaiClient(cred, "");

            CreateGroupRequest req = new CreateGroupRequest()
            {
                GroupName = "管理员",
                GroupId = "manager"
            };
            CreateGroupResponse resp = client.CreateGroup(req).
                ConfigureAwait(false).GetAwaiter().GetResult();
            string result = AbstractModel.ToJsonString(resp);
            // 输出json格式的字符串回包
            Console.WriteLine(result);
            return true;
        }
    }
}
