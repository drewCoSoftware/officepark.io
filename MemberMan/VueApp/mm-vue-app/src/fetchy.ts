// NOTE: This was copied from the RelatedSearch project.  We should find a way
// to generalize / share it between apps (NPM?)

// NOTE: This thingy could be used for any of our loading components....
export interface IStatusData {
  HasError: boolean,
  IsLoading: boolean,
  Message: string
}

export interface FetchyOptions {
  method: string | undefined,
  body?: any,
  headers?: {}
}

export interface IApiResponse {
  Code: number;
  Message: string;
}

export interface FetchyResponse<T extends IApiResponse> {
  Success: boolean;
  Data: T | null;
  Error: any | null;

  // TODO: We can care about headers, etc. later.
}


// ----------------------------------------------------------------------------------------------------------
export async function fetchy<T extends IApiResponse>(url: string, ops: FetchyOptions | null = null): Promise<FetchyResponse<T>> {
  // Populate default as needed.....
  if (ops == null) {
    ops = {
      method: 'GET'
    }
  }

  let res: FetchyResponse<T> = {
    Success: false,
    Data: null,
    Error: null
  }

  let p = fetch(url, {
    method: ops.method,
    body: ops.body,
    headers: ops.headers
  })

  let success = true;
  await p.then(response => {
    success = response.status == 200;   // OPTIONS: We could configure to pass/not other status codes.
    const res = response.json();
    return res;
  }).then(data => {

      // NOTE: This will not deserialize the date strings into proper
      // Date instances for typescript.
      // We may have to look at our intended property types from <T>
      // and find ways to convert from there.  Definitiely NOT something that
      // we want to mess with at this time.
      res.Data = <T>data;
      res.Success = success;

    }).catch((error) => {

      // Errors happen when there is some kind of network issue.
      res.Success = false;
      res.Error = error
    });

  return res;
}

