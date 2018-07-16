using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using Microsoft.VisualBasic.FileIO;

namespace MoveTo
{
    /// <summary>
    /// ファイル・フォルダの生成、検索、移動を行います
    /// </summary>
    public class TargetController
    {
        /// <summary>
        /// 移動対象ファイルが存在するかどうかを取得します。
        /// </summary>
        public int FileCount { get; private set; }

        /// <summary>
        /// 読み込んだファイル・フォルダのうち最新のものを取得します。
        /// 対象となるファイル・フォルダが存在しない場合には null を返します。
        /// </summary>
        public FileInfo LatestTargetInfo { get; private set; }

        /// <summary>
        /// 読み込んだファイル・フォルダのうち最古のものを取得します。
        /// 対象となるファイル・フォルダが存在しない場合には null を返します。
        /// </summary>
        public FileInfo OldestTargetInfo { get; private set; }

        /// <summary>
        /// 読み込んだファイル・フォルダ情報一覧を返します
        /// </summary>
        public List<FileInfo> TargetInfo { get; private set; }

        /// <summary>
        /// 最後に更新されたファイル・フォルダ名を取得します。
        /// 対象が存在しない場合には、"" (空文字) を返します。
        /// </summary>
        public string LatestTargetName { get; private set; }

        /// <summary>
        /// 最初に更新されたファイル・フォルダ名を取得します。
        /// 対象が存在しない場合には、"" (空文字) を返します。
        /// </summary>
        public string OldestTargetName { get; private set; }


        /// <summary>
        /// コンストラクタ
        /// </summary>
        public TargetController(string[] fileNames)
        {
            Contract.Requires(fileNames != null);
            Contract.Ensures(this.LatestTargetName != null);
            Contract.Ensures(this.OldestTargetName != null);

            InitializeTargets();
            GetFileInfo(fileNames);
        }

        private void InitializeTargets()
        {
            Contract.Ensures(this.TargetInfo.Count == 0);

            this.FileCount = 0;
            this.LatestTargetName = "";
            this.OldestTargetName = "";
            this.TargetInfo = new List<FileInfo>();
        }

        /// <summary>
        /// 管理下のファイル・フォルダを移動します。
        /// ファイルの移動が途中で失敗した場合でも
        /// ファイル情報(this.TargetInfo)はすべてリセットされます。
        /// </summary>
        /// <param name="destFolderName">移動先フォルダ</param>
        /// <returns>移動済みファイル・フォルダ数</returns>
        public int MoveTo(string destFolderName)
        {
            Contract.Requires(destFolderName != null);
            Contract.Requires(destFolderName.Length != 0);
            Contract.Ensures(this.TargetInfo.Count == 0);

            int count = 0;

            try
            {
                // 移動するファイル・フォルダがなくても移動先フォルダを作成
                Directory.CreateDirectory(destFolderName);

                if (this.TargetInfo.Count > 0)
                {
                    foreach (FileInfo info in this.TargetInfo)
                    {
                        // Win32 SHFileOperation関数 を利用
                        if((info.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                        {
                            FileSystem.MoveDirectory(info.FullName, Path.Combine(destFolderName, info.Name), UIOption.AllDialogs);
                        }
                        else
                        {
                            FileSystem.MoveFile(info.FullName, Path.Combine(destFolderName, info.Name), UIOption.AllDialogs);
                        }
                        count++;
                    }
                }
            }
            catch (COMException)
            {
                // おそらくキャンセル扱いなので無視
            }
            catch (Exception)
            {
                throw;
            }

            // すべて移動できたはずなので、リストを空にする
            InitializeTargets();

            return count;
        }

        /// <summary>
        /// 指定したファイル・フォルダの情報を取得します。
        /// 高速化のため、フォルダ内は検索しません。
        /// </summary>
        /// <param name="fileNames">対象とするファイル・フォルダ名</param>
        private void GetFileInfo(string[] fileNames)
        {
            Contract.Requires(fileNames != null);
            Contract.Ensures(this.LatestTargetName != null);
            Contract.Ensures(this.OldestTargetName != null);

            this.TargetInfo = new List<FileInfo>();
            for (int i = 0; i < fileNames.Length; i++)
            {
                try
                {
                    string name = fileNames[i];
                    if (name != null)
                    {
                        FileInfo info = new FileInfo(Path.GetFullPath(name));
                        this.TargetInfo.Add(info);
                    }
                }
                catch
                {
                    // 何らかのエラーが発生した場合は登録しない
                }
            }

            // ファイル情報を更新が新しい順にソート
            var targetInfo = from p in this.TargetInfo
                             orderby p.LastWriteTime descending
                             select p;
            this.FileCount = targetInfo.ToArray().Length;
            this.LatestTargetName = this.OldestTargetName = "";

            if (this.FileCount < 1)
            {
                this.LatestTargetInfo = this.OldestTargetInfo = null;

            }
            else
            {
                // 最新と最古のファイル情報を取り出し
                this.LatestTargetInfo = targetInfo.ToArray()[0];
                this.OldestTargetInfo = targetInfo.ToArray()[this.FileCount - 1];

                this.LatestTargetName = GetName(this.LatestTargetInfo);
                this.OldestTargetName = GetName(this.OldestTargetInfo);
            }
        }

        private string GetName(FileInfo info)
        {
            if ((info.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
            {
                // フォルダー名に . が含まれていた場合に正しく処理できるようにする
                return info.Name;
            }
            else
            {
                return Path.GetFileNameWithoutExtension(info.Name);
            }
        }

    }
}
