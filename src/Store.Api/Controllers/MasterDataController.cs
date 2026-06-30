using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Store.Api.Controllers;
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class MasterDataController : BaseApiController { }
