namespace Smartera.BLL.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        ICategoryRepository Categories { get; }
        IProductRepository Products { get; }
        ICartRepository Carts { get; }
        IOrderRepository Orders { get; }
        IPaymentRepository Payments { get; }
        IReviewRepository Reviews { get; }
        IApplicationUserRepository Users { get; }
        Task<int> CompleteAsync(CancellationToken cancellationToken = default);
    }
}
