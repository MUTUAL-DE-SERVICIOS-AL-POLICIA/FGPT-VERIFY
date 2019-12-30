using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dermalog.Afis.FingerCode3;
using System.IO;
using System.Windows;

namespace DermalogMultiScannerDemo
{
    public class LocalAFIS
    {
        private Dictionary<long, LocalUser> _userList;
        private long _nextId = 0;

        public string StoragePath
        {
            get
            {
                return LocalDB.StoragePath;
            }
        }

        public LocalAFIS()
        {
            _userList = LocalDB.convertFoldersToUserList();
            _nextId = getMaxId();
        }

        public bool IsEmpty()
        {
            return _userList.Count == 0;
        }

        #region ID Logic
        private long getMaxId()
        {
            if (_userList.Keys.Count == 0)
                return 0;
            return _userList.Keys.Max();
        }

        private long getNextId()
        {
            return ++_nextId;
        }
        #endregion

        #region User Database Logic
        public Dictionary<long, LocalUser> GetUserList()
        {
            return _userList;
        }

        public LocalUser RegisterUser(String name, List<Fingerprint> fingerprints)
        {
            LocalUser localUser = new LocalUser();
            localUser.ID = getNextId();
            localUser.Name = name;
            localUser.Fingerprints = fingerprints;

            _userList.Add(localUser.ID, localUser);

            LocalDB.createUserFolder(localUser);

            return localUser;
        }

        public AFISVerificationResult VerifyUser(long userId, List<Fingerprint> fingerprints, int threshold)
        {
            AFISVerificationResult result = new AFISVerificationResult();

            if (!_userList.ContainsKey(userId))
            {
                throw new Exception("USER NOT REGISTERED");
            }

            LocalUser user = _userList[userId];
            List<Fingerprint> userFingerprints = user.Fingerprints;

            double dMaxScore = 0.0;

            for (int i = 0; i < userFingerprints.Count; i++)
            {
                for (int j = 0; j < fingerprints.Count; j++)
                {
                    double dScore = new Matcher().Match(userFingerprints[i].Template, fingerprints[j].Template);

                    if (dScore > threshold && dScore > dMaxScore)
                    {
                        dMaxScore = dScore;
                        result.Score = dMaxScore;
                        result.Hit = true;
                    }
                }
            }

            return result;
        }





        public AFISVerificationResult muserpol_verifyUser(long userId, List<Fingerprint> fingerprints, int threshold, MainWindow o)
        {

//M3


            AFISVerificationResult result = new AFISVerificationResult();
            double dMaxScore = 0.0;

            
            if (!o.m_existFingerRegistry)
            {

                result.Score = 0;
                result.Hit = false;
                return result;
            }


         Dermalog.Afis.FingerCode3.Matcher matcher = new Dermalog.Afis.FingerCode3.Matcher();



         foreach (var f_template in o.m_fingers)
            {

                for (int j = 0; j < fingerprints.Count; j++)
                {
                    double dScore = new Matcher().Match(f_template, fingerprints[j].Template);
                    //double dScore = matcher.Match(f_template, fingerprints[j].Template);

                    if (dScore > threshold && dScore > dMaxScore)
                    {
                        dMaxScore = dScore;
                        result.Score = dMaxScore;
                        result.Hit = true;
                    }
                }
            }





            return result;
        }



        #endregion
    }

    public class AFISVerificationResult
    {
        private double _score;
        public double Score
        {
            get { return _score; }
            set { _score = value; }
        }

        private bool _hit = false;
        public bool Hit
        {
            get { return _hit; }
            set { _hit = value; }
        }
    }
}
