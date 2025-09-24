﻿using System;
using System.Runtime;

namespace MRP;

public interface IUserRepository
{
    public List<User> GetAll();

    public User? GetUserById(Guid id);

    public Guid AddUser(User user);
}