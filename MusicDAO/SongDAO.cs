﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models;

namespace MusicDAO
{
    public class SongDAO
    {

        private MusicPlayerAppContext _dbContext;
        private static SongDAO instance;

        public SongDAO()
        {
            _dbContext = new MusicPlayerAppContext();
        }

        public static SongDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new SongDAO();
                }
                return instance;
            }
        }


        public Song? GetOne(int id)
        {
            return _dbContext.Songs
                .SingleOrDefault(a => a.SongId.Equals(id));
        }

        public List<Song> GetAll()
        {
            return _dbContext.Songs
                .ToList();
        }

        public void Add(Song a)
        {
            Song? cur = GetOne(a.SongId);
            if (cur != null)
            {
                throw new Exception();
            }
            _dbContext.Songs.Add(a);
            _dbContext.SaveChanges();
        }

        public void Update(Song a)
        {
            Song? cur = GetOne(a.SongId);
            if (cur == null)
            {
                throw new Exception();
            }
            _dbContext.Entry(cur).CurrentValues.SetValues(a);
            _dbContext.SaveChanges();
        }

        public void Delete(int id)
        {
            Song? cur = GetOne(id);
            if (cur != null)
            {
                _dbContext.Songs.Remove(cur);
                _dbContext.SaveChanges();
            }
        }

    }
}
