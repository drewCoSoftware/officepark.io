// NOTE: This was copied from the RelatedSearch project.  We should find a way
// to generalize / share it between apps (NPM?)

// NOTE: This thingy could be used for any of our loading components....
export interface IStatusData {
  HasError: boolean,
  IsLoading: boolean,
  Message: string
}

export interface FetchyOptions
{
  Method: string | undefined
}

export interface IApiResponse { 
  Code: number;
  Message: string;
}

export interface FetchyResponse<T extends IApiResponse>
{
  Success: boolean;
  Fail: boolean;
  Data: T | null;
  Error: any | null;
  
  // TODO: We can care about headers, etc. later.
}


// ----------------------------------------------------------------------------------------------------------
export async function fetchy<T extends IApiResponse>(url:string, ops: FetchyOptions | null = null) : Promise<FetchyResponse<T>>
{
  // Populate default as needed.....
  if (ops == null) {
    ops = {
      Method: 'GET'
    }
  }

  let res:FetchyResponse<T> = {
    Success: false,
    Fail:false,
    Data: null,
    Error: null
  }

  let p = fetch(url, {
    method: ops.Method
  })

  await p.then(response => response.json())
  .then(data => {

    // NOTE: This will not deserialize the date strings into proper
    // Date instances for typescript.
    // We may have to look at our intended property types from <T>
    // and find ways to convert from there.  Definitiely NOT something that
    // we want to mess with at this time.
    res.Data = <T>data;
    res.Success = true;
    res.Fail = false;

  }).catch((error) => {
    res.Success = false;
    res.Fail = true;
    res.Error = error
  });
  
  // console.log('res is: ');
  // console.log(res.Data);
  return res;
}

