using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public class GetPaymentMethodByIdQueryHandler : IQueryHandler<GetPaymentMethodByIdQuery, PaymentMethodDto?>
    {
        private readonly IPaymentMethodRepository _repository;
        private readonly IMapper _mapper;

        public GetPaymentMethodByIdQueryHandler(IPaymentMethodRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<PaymentMethodDto?> Handle(GetPaymentMethodByIdQuery query, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(query.Id);
            return entity is null ? null : _mapper.Map<PaymentMethodDto>(entity);
        }
    }

    public class GetAllPaymentMethodsQueryHandler : IQueryHandler<GetAllPaymentMethodsQuery, List<PaymentMethodDto>>
    {
        private readonly IPaymentMethodRepository _repository;
        private readonly IMapper _mapper;

        public GetAllPaymentMethodsQueryHandler(IPaymentMethodRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<PaymentMethodDto>> Handle(GetAllPaymentMethodsQuery query, CancellationToken cancellationToken)
        {
            var entities = await _repository.GetAllAsync();
            return _mapper.Map<List<PaymentMethodDto>>(entities);
        }
    }
}
