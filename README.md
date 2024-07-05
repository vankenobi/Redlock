![Example Image](/image.png)

Redlock Restaurant Simulation

In this .NET Core console app, I aim to explain why we need to use Redis lock. If you review the code, you will see that I wrote two functions: Main and MakeReservation.

I simulated two clients attempting to reserve the same table at the same time. In the scenario without Redlock, both clients will see the table as available and both can successfully reserve it. However, this situation is a failure for us. If we use Redlock, a lock will be activated while the first client reserves the table. At the same time, if the second client tries to reserve it, they won't be able to access this data because the first client has locked this object.
