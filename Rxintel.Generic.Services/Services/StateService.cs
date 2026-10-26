using AutoMapper;
using Microsoft.Extensions.Logging;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Repositories.Interfaces;
using Rxintel.Generic.Services.Interfaces;

namespace Rxintel.Generic.Services.Services
{
    public class StateService : GenericService<StateDto, State>, IStateService
    {     

        public StateService(IStateRepository stateRepository, IMapper mapper, ILogger<StateService> logger): base(stateRepository, mapper, "StateId")
        {
           
        }
             
    }
}
