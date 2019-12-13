using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Dermalog.Afis.FingerCode3;

namespace DermalogMultiScannerDemo
{
    public class LocalDB
    {
        public static String StoragePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\DermalogMultiScannerDemo";
        private static String FILE_DEMOGRAPHIC = "user.txt";

        #region Folder-IO
        public static void makeDirectory()
        {
            if (!Directory.Exists(StoragePath))
                Directory.CreateDirectory(StoragePath);
        }

        public static void createUserFolder(LocalUser localUser)
        {
            String idString = localUser.ID.ToString("D6");
            String idPath = Path.Combine(StoragePath, idString);
            if (!Directory.Exists(idPath))
                Directory.CreateDirectory(idPath);

            String userPath = Path.Combine(idPath, FILE_DEMOGRAPHIC);
            StreamWriter sw = new StreamWriter(userPath);
            sw.Write(localUser.Name);
            sw.Flush();
            sw.Close();
            

            for (int i = 0; i < localUser.Fingerprints.Count; i++)
            {
                String templateString = String.Format("template{0}.dat",
                        localUser.Fingerprints[i].Position.ToString("D2"));
                String templatePath = Path.Combine(idPath, templateString);
                FileStream fs = File.Create(templatePath);
                byte[] data = localUser.Fingerprints[i].Template.GetData();
                fs.Write(data, 0, data.Length);
                fs.Flush();
                fs.Close();
            }
        }

        public static Dictionary<long, LocalUser> convertFoldersToUserList()
        {
            LocalDB.makeDirectory();

            Dictionary<long, LocalUser> userList = new Dictionary<long, LocalUser>();

            string[] dirs = Directory.GetDirectories(StoragePath);

            if (dirs == null || dirs.Length == 0)
                return userList;

            foreach (string dir in dirs)
            {
                LocalUser localUser = new LocalUser();
                String dirId = Path.GetFileName(dir);
                localUser.ID = long.Parse(dirId);

                StreamReader sr = new StreamReader(Path.Combine(dir, FILE_DEMOGRAPHIC));
                localUser.Name = sr.ReadToEnd();
                sr.Close();

                string[] templateFiles = Directory.GetFiles(dir, "template*");
                foreach (string templateFile in templateFiles)
                {
                    byte[] data = File.ReadAllBytes(templateFile);

                    String templateFileString = Path.GetFileNameWithoutExtension(templateFile);
                    String fingerPos = templateFileString.Substring(8, 2);

                    Fingerprint fingerprint = new Fingerprint();
                    fingerprint.Template = new Template();
                    fingerprint.Template.SetData(data, Dermalog.Afis.FingerCode3.Enums.TemplateFormat.Dermalog);
                    fingerprint.Position = UInt32.Parse(fingerPos);
                    localUser.Fingerprints.Add(fingerprint);
                }

                userList.Add(localUser.ID, localUser);
            }

            return userList;
        }
        #endregion
    }
}
